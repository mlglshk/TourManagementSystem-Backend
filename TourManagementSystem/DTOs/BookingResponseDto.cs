namespace TourManagementSystem.DTOs
{
    public class BookingResponseDto
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int TourScheduleId { get; set; }
        public int Participants { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SpecialRequirements { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Дополнительная информация для удобства
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? TourTitle { get; set; }
        public DateTime? TourStartTime { get; set; }
    }
}