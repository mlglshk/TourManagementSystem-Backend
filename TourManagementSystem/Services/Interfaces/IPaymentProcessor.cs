using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IPaymentProcessor
    {
        Task<PaymentResultDto> ProcessPaymentAsync(PaymentCreateDto paymentDto, decimal amount);
        Task LogPaymentHistoryAsync(int paymentId, string status, string? notes = null);
        PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto);
    }

}