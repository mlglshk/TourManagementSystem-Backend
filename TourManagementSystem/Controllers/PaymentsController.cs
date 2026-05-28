using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все методы требуют аутентификации
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;  // ← ДОБАВИТЬ
        private readonly ApplicationDbContext _context;

        public PaymentsController(
            IPaymentService paymentService,
            ILogger<PaymentsController> logger,               // ← ДОБАВИТЬ в конструктор
            ApplicationDbContext context)                     // ← ДОБАВИТЬ в конструктор
        {
            _paymentService = paymentService;
            _logger = logger;                                  // ← ДОБАВИТЬ
            _context = context;                                // ← ДОБАВИТЬ
        }

        // GET: api/payments
        [HttpGet]
        [Authorize(Roles = "Admin")] // Только админы
        public async Task<ActionResult<List<PaymentResponseDto>>> GetAllPayments()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        // GET: api/payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentResponseDto>> GetPayment(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/payments
        [HttpPost]
        [Authorize(Roles = "Tourist,Admin")] // Туристы и админы
        public async Task<ActionResult<PaymentResponseDto>> CreatePayment(PaymentCreateDto createDto)
        {
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(createDto);
                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/payments/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Только админы
        public async Task<ActionResult<PaymentResponseDto>> UpdatePayment(int id, PaymentUpdateDto updateDto)
        {
            try
            {
                var payment = await _paymentService.UpdatePaymentAsync(id, updateDto);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/payments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Только админы
        public async Task<ActionResult> DeletePayment(int id)
        {
            try
            {
                var result = await _paymentService.DeletePaymentAsync(id);

                if (!result)
                    return NotFound(new { message = "Платеж не найден" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ НОВЫЙ: Валидация платежных данных
        [HttpPost("validate")]
        [AllowAnonymous]
        public ActionResult ValidatePaymentData(PaymentCreateDto paymentDto)
        {
            try
            {
                var result = _paymentService.ValidatePaymentData(paymentDto);

                return Ok(new
                {
                    isValid = result.IsValid,
                    errors = result.Errors,
                    message = result.IsValid ? "Данные карты валидны" : "Обнаружены ошибки"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ НОВЫЙ: Получить историю платежа
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<PaymentHistoryDto>>> GetPaymentHistory(int id)
        {
            try
            {
                var history = await _paymentService.GetPaymentHistoryAsync(id);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ✅ НОВЫЙ: Повторная попытка платежа
        [HttpPost("{id}/retry")]
        [Authorize(Roles = "Tourist,Admin")]
        public async Task<ActionResult<PaymentResponseDto>> RetryPayment(int id, PaymentCreateDto createDto)
        {
            try
            {
                // Проверяем, что платеж существует и имеет статус Failed
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment.Status != "Failed")
                    return BadRequest(new { message = "Можно повторить только неудачный платеж" });

                // Создаем новый платеж с теми же данными
                var newPayment = await _paymentService.CreatePaymentAsync(createDto);
                return Ok(newPayment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ НОВЫЙ: Тестовый платеж (для разработки)
        [HttpPost("test")]
        [AllowAnonymous]
        public ActionResult TestPayment()
        {
            return Ok(new
            {
                message = "Платежная система работает",
                testCard = "4111 1111 1111 1111", // Тестовая карта Visa
                testExpiry = "12/25",
                testCvv = "123",
                testHolder = "TEST USER"
            });
        }


        // Добавить в конец класса PaymentsController
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentStatisticsDto>> GetPaymentStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var stats = await _paymentService.GetPaymentStatisticsAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка получения статистики платежей");
                return StatusCode(500, new { message = "Ошибка получения статистики" });
            }
        }

        // Простой дашборд (быстрый)
        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetDashboard()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek + 1);

            var stats = await _paymentService.GetPaymentStatisticsAsync(startOfMonth, now);

            // Добавляем информацию о бронированиях
            var totalBookings = await _context.Bookings.CountAsync();
            var pendingPayments = await _context.Payments.CountAsync(p => p.Status == "Pending" && p.CreatedAt > now.AddDays(-7));

            return Ok(new
            {
                period = new
                {
                    from = startOfMonth,
                    to = now,
                    days = (now - startOfMonth).Days
                },
                revenue = new
                {
                    monthly = stats.TotalRevenue,
                    averageCheck = stats.AverageCheck,
                    projected = stats.TotalRevenue / (now - startOfMonth).Days * 30
                },
                transactions = new
                {
                    total = stats.TotalTransactions,
                    success = stats.SuccessfulCount,
                    failed = stats.FailedCount,
                    pending = pendingPayments,
                    successRate = stats.SuccessRate
                },
                bookings = new
                {
                    total = totalBookings,
                    withPayment = await _context.Bookings.CountAsync(b => b.Payments.Any(p => p.Status == "Completed"))
                },
                daily = stats.DailyStats.Take(7)
            });
        }
    }
}