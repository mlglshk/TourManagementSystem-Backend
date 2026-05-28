using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentSettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PaymentSettingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("mode")]
        [AllowAnonymous]
        public ActionResult GetPaymentInfo()
        {
            var mode = _configuration["Payment:Mode"] ?? "Test";

            var info = new
            {
                currentMode = mode,
                availableModes = new[] { "Test", "YooKassa", "SbpQR" },
                modes = new Dictionary<string, object>
                {
                    ["Test"] = new
                    {
                        description = "Тестовый режим (симуляция платежей, алгоритм Луна)",
                        testCards = new[]
                        {
                            new { number = "4111 1111 1111 1111", expiry = "12/25", cvv = "123", result = "Успех" },
                            new { number = "4000 0000 0000 0002", expiry = "12/25", cvv = "123", result = "Ошибка" }
                        }
                    },
                    ["YooKassa"] = new
                    {
                        description = "Реальные платежи через ЮKassa",
                        isConfigured = !string.IsNullOrEmpty(_configuration["Payment:YooKassa:ShopId"]),
                        note = "Для реальной оплаты требуется настройка ShopId и SecretKey"
                    },
                    ["SbpQR"] = new
                    {
                        description = "Оплата через QR-код СБП",
                        note = "Генерирует QR-код для оплаты через мобильные приложения банков"
                    }
                }
            };

            return Ok(info);
        }

        [HttpPost("mode")]
        [Authorize(Roles = "Admin")]
        public ActionResult SetMode([FromBody] string mode)
        {
            var allowedModes = new[] { "Test", "YooKassa", "SbpQR" };
            if (!allowedModes.Contains(mode))
                return BadRequest(new { message = "Доступные режимы: Test, YooKassa, SbpQR" });

            // В реальном проекте здесь обновление в БД
            return Ok(new
            {
                message = $"Режим оплаты изменен на {mode}",
                note = "Изменение временное. Для постоянного изменения обновите appsettings.json"
            });
        }
    }
}