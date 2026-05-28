using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;


namespace TourManagementSystem.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;  // ← ДОБАВИТЬ ЭТУ СТРОКУ


        public BookingService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<BookingService> logger)  // ← ДОБАВИТЬ ЭТОТ ПАРАМЕТР
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
        }


        // ✅ ДОБАВЛЕНО: Получение всех бронирований
        public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(MapToDto).ToList();
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                throw new Exception("Бронирование не найдено");

            return MapToDto(booking);
        }

        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto createDto)
        {

            // Проверяем существование пользователя
            var user = await _context.Users.FindAsync(createDto.UserId);
            if (user == null)
                throw new Exception("Пользователь не найден");

            // Проверяем существование расписания тура
            var tourSchedule = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .FirstOrDefaultAsync(ts => ts.Id == createDto.TourScheduleId);

            if (tourSchedule == null)
                throw new Exception("Расписание тура не найдено");

            // Проверяем доступность мест (бизнес-логика бронирования)
            if (createDto.Participants > tourSchedule.AvailableSlots)
                throw new Exception($"Доступно только {tourSchedule.AvailableSlots} мест");

            // Генерируем уникальный номер бронирования
            var bookingNumber = GenerateBookingNumber();

            // Автоматический расчет стоимости (бизнес-логика)
            var totalPrice = tourSchedule.Price * createDto.Participants;

            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                UserId = createDto.UserId,
                TourScheduleId = createDto.TourScheduleId,
                Participants = createDto.Participants,
                TotalPrice = totalPrice,
                BookingDate = DateTime.UtcNow,
                Status = "Pending", // Фиксация статуса
                SpecialRequirements = createDto.SpecialRequirements
            };

            // Резервируем места
            tourSchedule.AvailableSlots -= createDto.Participants;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Отправляем письмо о создании бронирования
            try
            {
                await _emailService.SendBookingConfirmationAsync(booking.Id);
                Console.WriteLine($"Письмо о бронировании {booking.Id} отправлено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
            }
            return await GetBookingByIdAsync(booking.Id);
        }

        public async Task<bool> CancelBookingAsync(int id, string cancellationReason)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourSchedule)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null || booking.Status == "Cancelled")
                return false;

            // Возвращаем места в расписание
            booking.TourSchedule.AvailableSlots += booking.Participants;

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(MapToDto).ToList();
        }

        // ✅ ДОБАВЛЕНО: Обновление бронирования
        public async Task<BookingResponseDto> UpdateBookingAsync(int id, BookingUpdateDto updateDto)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourSchedule)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                throw new Exception("Бронирование не найдено");

            // сохраняем старый статус до изменений
            var oldStatus = booking.Status;

            // Обновляем количество участников (с проверкой доступности мест)
            if (updateDto.Participants.HasValue && updateDto.Participants.Value != booking.Participants)
            {
                var difference = updateDto.Participants.Value - booking.Participants;

                if (difference > 0 && difference > booking.TourSchedule.AvailableSlots)
                    throw new Exception($"Недостаточно мест. Доступно: {booking.TourSchedule.AvailableSlots}");

                booking.TourSchedule.AvailableSlots -= difference;
                booking.Participants = updateDto.Participants.Value;
                booking.TotalPrice = booking.TourSchedule.Price * booking.Participants;
            }

            // Обновляем статус
            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                booking.Status = updateDto.Status;

                // Если статус меняется на "Cancelled", возвращаем места
                if (updateDto.Status == "Cancelled" && booking.Status != "Cancelled")
                {
                    booking.TourSchedule.AvailableSlots += booking.Participants;
                    booking.CancelledAt = DateTime.UtcNow;
                }
                // Если статус меняется с "Cancelled" на другой, резервируем места обратно
                else if (booking.Status == "Cancelled" && updateDto.Status != "Cancelled")
                {
                    if (booking.Participants > booking.TourSchedule.AvailableSlots)
                        throw new Exception($"Недостаточно мест для восстановления бронирования. Доступно: {booking.TourSchedule.AvailableSlots}");

                    booking.TourSchedule.AvailableSlots -= booking.Participants;
                    booking.CancelledAt = null;
                }
            }

            // Обновляем специальные требования
            if (updateDto.SpecialRequirements != null)
                booking.SpecialRequirements = updateDto.SpecialRequirements;

            // Обновляем причину отмены
            if (updateDto.CancellationReason != null)
                booking.CancellationReason = updateDto.CancellationReason;

            await _context.SaveChangesAsync();
            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                await _notificationService.NotifyBookingStatusChangedAsync(id, oldStatus, updateDto.Status);
            }

            return await GetBookingByIdAsync(booking.Id);
        }

        // ✅ ДОБАВЛЕНО: Обновление только статуса бронирования
        public async Task<bool> UpdateBookingStatusAsync(int id, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.TourSchedule)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return false;

            var oldStatus = booking.Status;
            booking.Status = status;

            // Логика управления местами при изменении статуса
            if (oldStatus == "Cancelled" && status != "Cancelled")
            {
                // Восстановление бронирования - резервируем места
                if (booking.Participants > booking.TourSchedule.AvailableSlots)
                    throw new Exception($"Недостаточно мест для восстановления бронирования. Доступно: {booking.TourSchedule.AvailableSlots}");

                booking.TourSchedule.AvailableSlots -= booking.Participants;
                booking.CancelledAt = null;
                booking.CancellationReason = null;
            }
            else if (oldStatus != "Cancelled" && status == "Cancelled")
            {
                // Отмена бронирования - возвращаем места
                booking.TourSchedule.AvailableSlots += booking.Participants;
                booking.CancelledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.NotifyBookingStatusChangedAsync(id, oldStatus, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка создания уведомления для бронирования {id}");
            }
            return true;
        }

        // ✅ ДОБАВЛЕНО: Фильтрация по статусу
        public async Task<List<BookingResponseDto>> GetBookingsByStatusAsync(string status)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .Where(b => b.Status == status)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(MapToDto).ToList();
        }

        // ✅ ДОБАВЛЕНО: Фильтрация по email пользователя
        public async Task<List<BookingResponseDto>> GetBookingsByUserEmailAsync(string email)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .Where(b => b.User != null && b.User.Email.ToLower().Contains(email.ToLower()))
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(MapToDto).ToList();
        }

        // ✅ ДОБАВЛЕНО: Комбинированный поиск
        public async Task<List<BookingResponseDto>> SearchBookingsAsync(string? status, string? email)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(b => b.User != null && b.User.Email.ToLower().Contains(email.ToLower()));
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(MapToDto).ToList();
        }

        private string GenerateBookingNumber()
        {
            return $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private BookingResponseDto MapToDto(Booking booking)
        {
            return new BookingResponseDto
            {
                Id = booking.Id,
                BookingNumber = booking.BookingNumber,
                UserId = booking.UserId ?? 0,  // если null, то 0
                TourScheduleId = booking.TourScheduleId,
                Participants = booking.Participants,
                TotalPrice = booking.TotalPrice,
                BookingDate = booking.BookingDate,
                Status = booking.Status,
                SpecialRequirements = booking.SpecialRequirements,
                CancelledAt = booking.CancelledAt,
                CancellationReason = booking.CancellationReason,
                // Если пользователь есть — берем его данные, иначе показываем данные из запроса
                UserName = booking.User != null
                    ? $"{booking.User.FirstName} {booking.User.LastName}"
                    : "Гость",  // ← для гостей
                UserEmail = booking.User?.Email ?? "Гость без email",  // ← для гостей
                TourTitle = booking.TourSchedule?.Tour?.Title,
                TourStartTime = booking.TourSchedule?.StartTime
            };
        }


        /// <summary>
        /// Создание бронирования администратором для клиента без аккаунта
        /// </summary>
        /// <summary>
        /// <summary>
        /// Создание бронирования администратором для клиента без аккаунта
        /// </summary>
        public async Task<BookingResponseDto> CreateAdminBookingWithoutAccountAsync(AdminBookingCreateDto createDto)
        {
            // 1. Проверяем расписание тура
            var tourSchedule = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .FirstOrDefaultAsync(ts => ts.Id == createDto.TourScheduleId);

            if (tourSchedule == null)
                throw new Exception("Расписание тура не найдено");

            // 2. Проверяем места
            if (createDto.Participants > tourSchedule.AvailableSlots)
                throw new Exception($"Доступно только {tourSchedule.AvailableSlots} мест");

            // 3. Ищем существующего пользователя
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == createDto.UserEmail && u.IsActive);

            int? userIdForBooking = null;  // ← null для гостей

            if (existingUser != null)
            {
                userIdForBooking = existingUser.Id;
                _logger.LogInformation($"Бронирование для существующего пользователя: {existingUser.Email}");
            }
            else
            {
                _logger.LogInformation($"Бронирование для гостя: {createDto.UserEmail} (без привязки к пользователю)");
            }

            // 4. Создаем бронирование
            var bookingNumber = GenerateBookingNumber();
            var totalPrice = tourSchedule.Price * createDto.Participants;

            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                UserId = userIdForBooking,  // ← null для гостей!
                TourScheduleId = createDto.TourScheduleId,
                Participants = createDto.Participants,
                TotalPrice = totalPrice,
                BookingDate = DateTime.UtcNow,
                Status = "Confirmed",
                SpecialRequirements = createDto.SpecialRequirements
            };

            // 5. Резервируем места
            tourSchedule.AvailableSlots -= createDto.Participants;

            // 6. Сохраняем
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Создано бронирование {bookingNumber} для {createDto.UserEmail}");

            // 7. Отправляем email
            try
            {
                await _emailService.SendBookingConfirmationAsync(booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Не удалось отправить email");
            }

            return MapToDto(booking);
        }

    }
}