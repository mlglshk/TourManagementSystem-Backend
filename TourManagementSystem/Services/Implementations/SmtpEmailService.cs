// TourManagementSystem/Services/Implementations/SmtpEmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpSettings _smtpSettings;

        public SmtpEmailService(
            ApplicationDbContext context,
            ILogger<SmtpEmailService> logger,
            IOptions<SmtpSettings> smtpSettings)
        {
            _context = context;
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<bool> SendEmailAsync(EmailSendDto emailDto)
        {
            try
            {
                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                    Subject = emailDto.Subject,
                    Body = emailDto.Body,
                    IsBodyHtml = emailDto.IsHtml
                };

                mailMessage.To.Add(emailDto.ToEmail);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent to {emailDto.ToEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {emailDto.ToEmail}");
                return false;
            }
        }

        public async Task<bool> SendTemplateEmailAsync(TemplateEmailSendDto templateEmailDto)
        {
            try
            {
                // Получаем шаблон из базы
                var template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.TemplateName == templateEmailDto.TemplateName && t.IsActive);

                if (template == null)
                {
                    _logger.LogWarning($"Template '{templateEmailDto.TemplateName}' not found or inactive");
                    return false;
                }

                // Заменяем плейсхолдеры в шаблоне
                var body = template.Body;
                var subject = template.Subject;

                foreach (var data in templateEmailDto.TemplateData)
                {
                    body = body.Replace($"{{{{{data.Key}}}}}", data.Value);
                    subject = subject.Replace($"{{{{{data.Key}}}}}", data.Value);
                }

                // Отправляем email
                var emailDto = new EmailSendDto
                {
                    ToEmail = templateEmailDto.ToEmail,
                    Subject = subject,
                    Body = body,
                    IsHtml = true
                };

                return await SendEmailAsync(emailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending template email '{templateEmailDto.TemplateName}'");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
        {
            var templateData = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "CurrentDate", DateTime.UtcNow.ToString("dd.MM.yyyy") }
            };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = email,
                TemplateName = "Welcome",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

        public async Task<bool> SendBookingConfirmationAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.User == null)
                return false;

            var templateData = new Dictionary<string, string>
            {
                { "UserName", $"{booking.User.FirstName} {booking.User.LastName}" },
                { "BookingNumber", booking.BookingNumber },
                { "TourTitle", booking.TourSchedule?.Tour?.Title ?? "Название тура" },
                { "StartTime", booking.TourSchedule?.StartTime.ToString("dd.MM.yyyy HH:mm") ?? "Не указано" },
                { "Participants", booking.Participants.ToString() },
                { "TotalPrice", booking.TotalPrice.ToString("C") }
            };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = booking.User.Email,
                TemplateName = "BookingConfirmation",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

        public async Task<bool> SendPaymentConfirmationAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null || payment.Booking?.User == null)
                return false;

            var templateData = new Dictionary<string, string>
            {
                { "UserName", $"{payment.Booking.User.FirstName} {payment.Booking.User.LastName}" },
                { "Amount", payment.Amount.ToString("C") },
                { "PaymentDate", payment.PaymentDate.ToString("dd.MM.yyyy HH:mm") },
                { "TransactionId", payment.TransactionId ?? "Не указан" }
            };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = payment.Booking.User.Email,
                TemplateName = "PaymentConfirmation",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

        public async Task<bool> SendTourReminderAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.User == null || booking.TourSchedule == null)
                return false;

            var templateData = new Dictionary<string, string>
            {
                { "UserName", $"{booking.User.FirstName} {booking.User.LastName}" },
                { "TourTitle", booking.TourSchedule.Tour?.Title ?? "Название тура" },
                { "StartTime", booking.TourSchedule.StartTime.ToString("dd.MM.yyyy HH:mm") },
                { "Location", booking.TourSchedule.Tour?.Location ?? "Место встречи" },
                { "Participants", booking.Participants.ToString() }
            };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = booking.User.Email,
                TemplateName = "TourReminder",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        

        }

        // Добавить в класс SmtpEmailService
        public async Task<bool> SendPaymentSuccessEmailAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment?.Booking?.User == null) return false;

            var templateData = new Dictionary<string, string>
    {
        { "UserName", $"{payment.Booking.User.FirstName} {payment.Booking.User.LastName}" },
        { "TourTitle", payment.Booking.TourSchedule?.Tour?.Title ?? "Тур" },
        { "Amount", payment.Amount.ToString("N2") },
        { "PaymentDate", payment.PaymentDate.ToString("dd.MM.yyyy HH:mm") },
        { "TransactionId", payment.TransactionId ?? "—" },
        { "BookingNumber", payment.Booking.BookingNumber }
    };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = payment.Booking.User.Email,
                TemplateName = "PaymentSuccess",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

        public async Task<bool> SendPaymentFailedEmailAsync(int paymentId, string errorMessage)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.TourSchedule)
                        .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment?.Booking?.User == null) return false;

            var templateData = new Dictionary<string, string>
    {
        { "UserName", $"{payment.Booking.User.FirstName} {payment.Booking.User.LastName}" },
        { "TourTitle", payment.Booking.TourSchedule?.Tour?.Title ?? "Тур" },
        { "Amount", payment.Amount.ToString("N2") },
        { "ErrorMessage", errorMessage }
    };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = payment.Booking.User.Email,
                TemplateName = "PaymentFailed",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

        public async Task<bool> SendBookingConfirmedEmailAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourSchedule)
                    .ThenInclude(ts => ts.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking?.User == null) return false;

            var templateData = new Dictionary<string, string>
    {
        { "UserName", $"{booking.User.FirstName} {booking.User.LastName}" },
        { "TourTitle", booking.TourSchedule?.Tour?.Title ?? "Тур" },
        { "BookingNumber", booking.BookingNumber },
        { "TourStartDate", booking.TourSchedule?.StartTime.ToString("dd.MM.yyyy HH:mm") ?? "—" },
        { "Participants", booking.Participants.ToString() },
        { "MeetingPoint", booking.TourSchedule?.Tour?.Location ?? "уточняется" }
    };

            var templateEmail = new TemplateEmailSendDto
            {
                ToEmail = booking.User.Email,
                TemplateName = "BookingConfirmed",
                TemplateData = templateData
            };

            return await SendTemplateEmailAsync(templateEmail);
        }

    }

    // Настройки SMTP
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Tour Management System";
        public bool EnableSsl { get; set; } = true;
    }
}