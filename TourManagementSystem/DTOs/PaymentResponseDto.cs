namespace TourManagementSystem.DTOs
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Дополнительная информация
        public string? BookingNumber { get; set; }
        public string? UserName { get; set; }
        public string? TourTitle { get; set; }
        public decimal? BookingTotalPrice { get; set; }
    }
}