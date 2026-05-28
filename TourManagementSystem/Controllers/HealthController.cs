using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;
using TourManagementSystem.Helpers;  // ← ЭТО ВАЖНО!


namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public HealthController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("test-email")]
        [AllowAnonymous]
        public async Task<IActionResult> TestEmail()
        {
            var result = await _emailService.SendEmailAsync(new EmailSendDto
            {
                ToEmail = "3vavavav3@gmail.com", // ваш email
                Subject = "🧪 Тест SMTP из TourManagementSystem",
                Body = @"
                <h1>✅ SMTP работает!</h1>
                <p>Поздравляю! Настройка почты выполнена правильно.</p>
                <p>Время: " + DateTime.Now + @"</p>
                <hr>
                <small>Tour Management System</small>
            ",
                IsHtml = true
            });

            if (result)
                return Ok(new { message = "✅ Email отправлен успешно!", to = "3vavavav3@gmail.com" });
            else
                return StatusCode(500, new { message = "❌ Ошибка отправки email" });
        }


        [HttpGet("payment-stats-test")]
        [AllowAnonymous]
        public async Task<IActionResult> TestPaymentStats([FromServices] IPaymentService paymentService)
        {
            var stats = await paymentService.GetPaymentStatisticsAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            return Ok(new
            {
                message = "Статистика платежей за 30 дней",
                stats = new
                {
                    revenue = stats.TotalRevenue,
                    successRate = stats.SuccessRate,
                    totalTransactions = stats.TotalTransactions,
                    averageCheck = stats.AverageCheck
                },
                daily = stats.DailyStats.Take(7).Select(d => new { d.Date, d.Count, d.Amount })
            });
        }

        [HttpGet("generate-qr")]
        [AllowAnonymous]
        public IActionResult GenerateTestQr([FromQuery] string text = "Тестовая оплата")
        {
            var qrBytes = QrCodeHelper.GeneratePng(text);
            return File(qrBytes, "image/png");
        }
    }
}
