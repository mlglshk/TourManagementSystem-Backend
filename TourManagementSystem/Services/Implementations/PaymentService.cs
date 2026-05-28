using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;


namespace TourManagementSystem.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEmailService _emailService;  // ← ДОБАВИТЬ ЭТУ СТРОКУ

        public PaymentService(
            ApplicationDbContext context,
            IPaymentProcessor paymentProcessor,
            ILogger<PaymentService> logger,
            IEmailService emailService)  // ← ДОБАВИТЬ В ПАРАМЕТРЫ
        {
            _context = context;
            _paymentProcessor = paymentProcessor;
            _logger = logger;
            _emailService = emailService;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
        }

        // Обновленный метод создания платежа
        public async Task<PaymentResponseDto> CreatePaymentAsync(PaymentCreateDto createDto)
        {
            _logger.LogInformation($"Создание платежа для бронирования {createDto.BookingId}");

            // Проверяем существование бронирования
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(b => b.Id == createDto.BookingId);

            if (booking == null)
                throw new Exception("Бронирование не найдено");

            // Проверяем статус бронирования
            if (booking.Status == "Cancelled")
                throw new Exception("Нельзя создать платеж для отмененного бронирования");

            // Проверяем сумму платежа
            if (createDto.Amount > booking.TotalPrice)
                throw new Exception($"Сумма платежа не может превышать общую стоимость бронирования ({booking.TotalPrice})");

            // Создаем запись о платеже
            var payment = new Payment
            {
                BookingId = createDto.BookingId,
                Amount = createDto.Amount,
                PaymentMethod = createDto.PaymentMethod,
                Status = "Pending",
                TransactionId = null,
                Notes = createDto.Notes,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Логируем начало обработки
            await _paymentProcessor.LogPaymentHistoryAsync(payment.Id, "Pending", "Создан новый платеж");

            try
            {
                // Меняем статус на обработку
                payment.Status = "Processing";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _paymentProcessor.LogPaymentHistoryAsync(payment.Id, "Processing", "Начало обработки платежа");

                // Обрабатываем платеж
                var paymentResult = await _paymentProcessor.ProcessPaymentAsync(createDto, createDto.Amount);

                if (paymentResult.Success)
                {
                    // Успешный платеж
                    payment.Status = "Completed";
                    payment.TransactionId = paymentResult.TransactionId;
                    payment.PaymentDate = DateTime.UtcNow;

                    await _paymentProcessor.LogPaymentHistoryAsync(payment.Id, "Completed",
                        $"Платеж успешен. TransactionId: {paymentResult.TransactionId}");

                    // ✅ ОТПРАВЛЯЕМ УВЕДОМЛЕНИЯ
                    try
                    {
                        // Отправляем письмо об успешной оплате
                        await _emailService.SendPaymentSuccessEmailAsync(payment.Id);
                        _logger.LogInformation($"Отправлено письмо об успешной оплате для платежа {payment.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка отправки письма об успешной оплате {payment.Id}");
                    }

                    // Обновляем статус бронирования, если оплачено полностью
                    var totalPaid = await _context.Payments
                        .Where(p => p.BookingId == payment.BookingId && p.Status == "Completed")
                        .SumAsync(p => p.Amount);

                    if (totalPaid >= booking.TotalPrice)
                    {
                        booking.Status = "Confirmed";

                        // ✅ ОТПРАВЛЯЕМ ПОДТВЕРЖДЕНИЕ БРОНИРОВАНИЯ
                        try
                        {
                            await _emailService.SendBookingConfirmedEmailAsync(booking.Id);
                            _logger.LogInformation($"Отправлено подтверждение бронирования {booking.Id}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Ошибка отправки подтверждения бронирования {booking.Id}");
                        }

                        _logger.LogInformation($"Бронирование {booking.Id} подтверждено после оплаты");
                    }
                }
                else
                {
                    // Неудачный платеж
                    payment.Status = "Failed";
                    payment.Notes = string.IsNullOrEmpty(payment.Notes)
                        ? $"Ошибка платежа: {paymentResult.ErrorMessage}"
                        : $"{payment.Notes}. Ошибка платежа: {paymentResult.ErrorMessage}";

                    await _paymentProcessor.LogPaymentHistoryAsync(payment.Id, "Failed",
                        $"Платеж отклонен: {paymentResult.ErrorMessage}");

                    // ✅ ОТПРАВЛЯЕМ ПИСЬМО ОБ ОШИБКЕ
                    try
                    {
                        await _emailService.SendPaymentFailedEmailAsync(payment.Id, paymentResult.ErrorMessage ?? "Неизвестная ошибка");
                        _logger.LogInformation($"Отправлено письмо об ошибке платежа {payment.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка отправки письма об ошибке платежа {payment.Id}");
                    }
                }

                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetPaymentByIdAsync(payment.Id);
            }
            catch (Exception ex)
            {
                // Ошибка при обработке
                payment.Status = "Failed";
                payment.Notes = $"Ошибка обработки: {ex.Message}";
                await _paymentProcessor.LogPaymentHistoryAsync(payment.Id, "Failed",
                    $"Исключение при обработке: {ex.Message}");

                await _context.SaveChangesAsync();
                throw new Exception($"Ошибка обработки платежа: {ex.Message}");
            }
        }

        // Метод для получения истории платежей
        public async Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int paymentId)
        {
            var history = await _context.PaymentHistories
                .Where(h => h.PaymentId == paymentId)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new PaymentHistoryDto
                {
                    Id = h.Id,
                    PaymentId = h.PaymentId,
                    Status = h.Status,
                    Notes = h.Notes,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return history;
        }

        // Метод для валидации платежных данных - КЛЮЧЕВОЙ МЕТОД!
        public PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto)
        {
            // Делегируем валидацию платежному процессору
            return _paymentProcessor.ValidatePaymentData(paymentDto);
        }

        public async Task<List<PaymentResponseDto>> GetAllPaymentsAsync()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<PaymentResponseDto> GetPaymentByIdAsync(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                throw new Exception("Платеж не найден");

            return MapToDto(payment);
        }

        public async Task<PaymentResponseDto> UpdatePaymentAsync(int id, PaymentUpdateDto updateDto)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                throw new Exception("Платеж не найден");

            // Нельзя изменять завершенные или возвращенные платежи
            if (payment.Status == "Completed" || payment.Status == "Refunded")
                throw new Exception($"Нельзя изменить платеж со статусом '{payment.Status}'");

            if (updateDto.Amount.HasValue)
                payment.Amount = updateDto.Amount.Value;

            if (!string.IsNullOrEmpty(updateDto.PaymentMethod))
                payment.PaymentMethod = updateDto.PaymentMethod;

            if (!string.IsNullOrEmpty(updateDto.Status))
                payment.Status = updateDto.Status;

            if (updateDto.TransactionId != null)
                payment.TransactionId = updateDto.TransactionId;

            if (updateDto.Notes != null)
                payment.Notes = updateDto.Notes;

            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetPaymentByIdAsync(payment.Id);
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
                return false;

            // Нельзя удалить завершенные платежи
            if (payment.Status == "Completed")
                throw new Exception("Нельзя удалить завершенный платеж");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ProcessPaymentAsync(int id, string transactionId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null || payment.Status != "Pending")
                return false;

            payment.Status = "Completed";
            payment.TransactionId = transactionId;
            payment.PaymentDate = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            // Обновляем статус бронирования, если оплачено полностью
            var totalPaid = await _context.Payments
                .Where(p => p.BookingId == payment.BookingId && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            if (totalPaid >= payment.Booking.TotalPrice)
            {
                payment.Booking.Status = "Confirmed";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkPaymentAsFailedAsync(int id, string reason)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null || payment.Status != "Pending")
                return false;

            payment.Status = "Failed";
            payment.Notes = string.IsNullOrEmpty(payment.Notes)
                ? $"Payment failed: {reason}"
                : $"{payment.Notes}. Payment failed: {reason}";
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RefundPaymentAsync(int id, string reason)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null || payment.Status != "Completed")
                return false;

            payment.Status = "Refunded";
            payment.Notes = string.IsNullOrEmpty(payment.Notes)
                ? $"Payment refunded: {reason}"
                : $"{payment.Notes}. Payment refunded: {reason}";
            payment.UpdatedAt = DateTime.UtcNow;

            // Если бронирование было подтверждено из-за этого платежа, меняем статус обратно
            var totalCompletedPayments = await _context.Payments
                .Where(p => p.BookingId == payment.BookingId && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            if (totalCompletedPayments - payment.Amount < payment.Booking.TotalPrice)
            {
                payment.Booking.Status = "Pending";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PaymentResponseDto>> GetPaymentsByBookingAsync(int bookingId)
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<List<PaymentResponseDto>> GetPaymentsByUserAsync(int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .Where(p => p.Booking.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<List<PaymentResponseDto>> GetPaymentsByStatusAsync(string status)
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<List<PaymentResponseDto>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments
                .Where(p => p.Status == "Completed");

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);

            return await query.SumAsync(p => p.Amount);
        }

        private PaymentResponseDto MapToDto(Payment payment)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                Notes = payment.Notes,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                BookingNumber = payment.Booking?.BookingNumber,
                UserName = payment.Booking?.User != null
                    ? $"{payment.Booking.User.FirstName} {payment.Booking.User.LastName}"
                    : null,
                TourTitle = payment.Booking?.TourSchedule?.Tour?.Title,
                BookingTotalPrice = payment.Booking?.TotalPrice
            };
        }

        // Добавить в конец класса PaymentService
        public async Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            var payments = await query.ToListAsync();

            // Основная статистика
            var totalRevenue = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            var successfulCount = payments.Count(p => p.Status == "Completed");
            var failedCount = payments.Count(p => p.Status == "Failed");
            var pendingCount = payments.Count(p => p.Status == "Pending");
            var refundedCount = payments.Count(p => p.Status == "Refunded");
            var totalTransactions = payments.Count;

            // Статистика по методам оплаты
            var methodStats = payments
                .Where(p => p.Status == "Completed")
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(
                    g => g.Key,
                    g => new PaymentMethodStats
                    {
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.Amount),
                        Percentage = (decimal)(successfulCount > 0
                            ? (double)g.Count() / successfulCount * 100
                            : 0)
                    }
                );

            // Ежедневная динамика (последние 7 дней)
            var dailyStats = new List<DailyPaymentStats>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                var dayPayments = payments.Where(p => p.CreatedAt.Date == day);

                dailyStats.Add(new DailyPaymentStats
                {
                    Date = day,
                    Count = dayPayments.Count(),
                    Amount = dayPayments.Where(p => p.Status == "Completed").Sum(p => p.Amount)
                });
            }

            return new PaymentStatisticsDto
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                SuccessfulCount = successfulCount,
                FailedCount = failedCount,
                PendingCount = pendingCount,
                RefundedCount = refundedCount,
                SuccessRate = totalTransactions > 0
                    ? (double)successfulCount / totalTransactions * 100
                    : 0,
                AverageCheck = successfulCount > 0
                    ? totalRevenue / successfulCount
                    : 0,
                PaymentMethods = methodStats,
                DailyStats = dailyStats
            };
        }

    }
}