// TourManagementSystem/Controllers/EmailsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailsController> _logger;

        public EmailsController(IEmailService emailService, ILogger<EmailsController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // POST: api/emails/send
        [HttpPost("send")]
        public async Task<ActionResult> SendEmail([FromBody] EmailSendDto emailDto)
        {
            try
            {
                var result = await _emailService.SendEmailAsync(emailDto);

                if (!result)
                    return BadRequest(new { message = "Ошибка отправки email" });

                return Ok(new { message = "Email отправлен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке email");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/emails/send-template
        [HttpPost("send-template")]
        public async Task<ActionResult> SendTemplateEmail([FromBody] TemplateEmailSendDto templateEmailDto)
        {
            try
            {
                var result = await _emailService.SendTemplateEmailAsync(templateEmailDto);

                if (!result)
                    return BadRequest(new { message = "Ошибка отправки шаблонного email" });

                return Ok(new { message = "Шаблонный email отправлен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке шаблонного email");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/emails/send-welcome
        [HttpPost("send-welcome")]
        public async Task<ActionResult> SendWelcomeEmail([FromBody] string email)
        {
            try
            {
                // Для теста используем "Test User"
                var result = await _emailService.SendWelcomeEmailAsync(email, "Test User");

                if (!result)
                    return BadRequest(new { message = "Ошибка отправки welcome email" });

                return Ok(new { message = "Welcome email отправлен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке welcome email");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/emails/send-booking-confirmation/5
        [HttpPost("send-booking-confirmation/{bookingId}")]
        public async Task<ActionResult> SendBookingConfirmation(int bookingId)
        {
            try
            {
                var result = await _emailService.SendBookingConfirmationAsync(bookingId);

                if (!result)
                    return BadRequest(new { message = "Ошибка отправки подтверждения бронирования" });

                return Ok(new { message = "Подтверждение бронирования отправлено успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке подтверждения бронирования {bookingId}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/emails/send-payment-confirmation/5
        [HttpPost("send-payment-confirmation/{paymentId}")]
        public async Task<ActionResult> SendPaymentConfirmation(int paymentId)
        {
            try
            {
                var result = await _emailService.SendPaymentConfirmationAsync(paymentId);

                if (!result)
                    return BadRequest(new { message = "Ошибка отправки подтверждения платежа" });

                return Ok(new { message = "Подтверждение платежа отправлено успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке подтверждения платежа {paymentId}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/emails/test
        [HttpPost("test")]
        [AllowAnonymous] // Для тестирования
        public async Task<ActionResult> TestEmail()
        {
            try
            {
                var testEmail = new EmailSendDto
                {
                    ToEmail = "test@example.com",
                    Subject = "Test Email from Tour Management System",
                    Body = "<h1>Test Email</h1><p>Это тестовое письмо от системы управления турами.</p>",
                    IsHtml = true
                };

                var result = await _emailService.SendEmailAsync(testEmail);

                if (!result)
                    return BadRequest(new { message = "Тестовый email не отправлен" });

                return Ok(new { message = "Тестовый email отправлен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке тестового email");
                return StatusCode(500, new { message = $"Внутренняя ошибка сервера: {ex.Message}" });
            }
        }
    }
}