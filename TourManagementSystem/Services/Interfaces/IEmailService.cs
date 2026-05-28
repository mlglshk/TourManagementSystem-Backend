// TourManagementSystem/Services/Interfaces/IEmailService.cs
using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailSendDto emailDto);
        Task<bool> SendTemplateEmailAsync(TemplateEmailSendDto templateEmailDto);

        // Автоматические рассылки
        Task<bool> SendWelcomeEmailAsync(string email, string userName);
        Task<bool> SendBookingConfirmationAsync(int bookingId);
        Task<bool> SendPaymentConfirmationAsync(int paymentId);
        Task<bool> SendTourReminderAsync(int bookingId);
        Task<bool> SendPaymentSuccessEmailAsync(int paymentId);
        Task<bool> SendPaymentFailedEmailAsync(int paymentId, string errorMessage);
        Task<bool> SendBookingConfirmedEmailAsync(int bookingId);
    }
}