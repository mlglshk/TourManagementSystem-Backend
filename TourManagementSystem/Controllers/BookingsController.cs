using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Implementations;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все методы требуют аутентификации
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService; // ✅ ДОБАВЬ dependency injection

        public BookingsController(IBookingService bookingService, IUserService userService)
        {
            _bookingService = bookingService;
            _userService = userService; // ✅ ИНИЦИАЛИЗИРУЕМ
        }

        // ✅ ДОБАВЛЕНО: GET: api/bookings - Получение всех бронирований (только для админов)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<BookingResponseDto>>> GetAllBookings()
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(bookings);
        }

        // ✅ ДОБАВЛЕНО: GET: api/bookings/search - Поиск и фильтрация бронирований
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<BookingResponseDto>>> SearchBookings(
            [FromQuery] string? status,
            [FromQuery] string? email)
        {
            var bookings = await _bookingService.SearchBookingsAsync(status, email);
            return Ok(bookings);
        }


        // POST: api/bookings - Создание бронирования
        // ✅ ДЛЯ ТУРИСТОВ: создание бронирования для себя
        [HttpPost("my")]
        [Authorize(Roles = "Tourist")]
        public async Task<ActionResult<BookingResponseDto>> CreateMyBooking(BookingCreateDto createDto)
        {
            try
            {
                // Автоматически берем ID текущего пользователя
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Игнорируем UserId из DTO, используем текущего пользователя
                createDto.UserId = currentUserId;

                var booking = await _bookingService.CreateBookingAsync(createDto);
                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ ДЛЯ АДМИНОВ: создание бронирования для любого пользователя
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BookingResponseDto>> CreateAdminBooking(BookingCreateDto createDto)
        {
            try
            {
                // Админ должен явно указать UserId
                if (createDto.UserId <= 0)
                    return BadRequest(new { message = "Admin must specify UserId" });

                // Проверяем существование пользователя
                try
                {
                    await _userService.GetUserByIdAsync(createDto.UserId);
                }
                catch
                {
                    return BadRequest(new { message = "User not found" });
                }

                var booking = await _bookingService.CreateBookingAsync(createDto);
                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/bookings/5 - Получение бронирования по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);

                // Проверяем, что пользователь имеет доступ к этому бронированию
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (booking.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid(); // Запрещаем доступ к чужим бронированиям
                }

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: api/bookings/user/5 - Бронирования пользователя
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<BookingResponseDto>>> GetUserBookings(int userId)
        {
            // Проверяем, что пользователь запрашивает свои бронирования
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var bookings = await _bookingService.GetUserBookingsAsync(userId);
            return Ok(bookings);
        }

        // ✅ ДОБАВЛЕНО: PUT: api/bookings/5 - Обновление бронирования
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BookingResponseDto>> UpdateBooking(int id, BookingUpdateDto updateDto)
        {
            try
            {
                var updatedBooking = await _bookingService.UpdateBookingAsync(id, updateDto);
                return Ok(updatedBooking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ ДОБАВЛЕНО: PATCH: api/bookings/5/status - Обновление статуса бронирования
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateBookingStatus(int id, [FromBody] string status)
        {
            try
            {
                var result = await _bookingService.UpdateBookingStatusAsync(id, status);

                if (!result)
                    return NotFound(new { message = "Бронирование не найдено" });

                return Ok(new { message = "Статус бронирования обновлен" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/bookings/5/cancel - Отмена бронирования
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelBooking(int id, [FromBody] string cancellationReason)
        {
            try
            {
                // Проверяем права доступа
                var booking = await _bookingService.GetBookingByIdAsync(id);
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (booking.UserId != currentUserId && currentUserRole != "Admin")
                {
                    return Forbid();
                }

                var result = await _bookingService.CancelBookingAsync(id, cancellationReason);

                if (!result)
                    return NotFound(new { message = "Бронирование не найдено или уже отменено" });

                return Ok(new { message = "Бронирование отменено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/bookings/admin/create-without-account
        // Создание бронирования администратором для клиента без аккаунта
        // POST: api/bookings/admin/create-without-account
        [HttpPost("admin/create-without-account")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BookingResponseDto>> CreateAdminBookingWithoutAccount([FromBody] AdminBookingCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var booking = await _bookingService.CreateAdminBookingWithoutAccountAsync(createDto);
                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}