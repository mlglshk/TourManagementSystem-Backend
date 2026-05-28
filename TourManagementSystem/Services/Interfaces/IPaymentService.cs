using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<List<PaymentResponseDto>> GetAllPaymentsAsync();
        Task<PaymentResponseDto> GetPaymentByIdAsync(int id);
        Task<PaymentResponseDto> CreatePaymentAsync(PaymentCreateDto createDto);
        Task<PaymentResponseDto> UpdatePaymentAsync(int id, PaymentUpdateDto updateDto);
        Task<bool> DeletePaymentAsync(int id);
        Task<bool> ProcessPaymentAsync(int id, string transactionId);
        Task<bool> MarkPaymentAsFailedAsync(int id, string reason);
        Task<bool> RefundPaymentAsync(int id, string reason);
        Task<List<PaymentResponseDto>> GetPaymentsByBookingAsync(int bookingId);
        Task<List<PaymentResponseDto>> GetPaymentsByUserAsync(int userId);
        Task<List<PaymentResponseDto>> GetPaymentsByStatusAsync(string status);
        Task<List<PaymentResponseDto>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);

        // ✅ ДОБАВЛЕННЫЕ МЕТОДЫ
        Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int paymentId);
        PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto);

        Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}