namespace TourManagementSystem.DTOs
{
    public class TourScheduleResponseDto
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AvailableSlots { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Дополнительная информация
        public string? TourTitle { get; set; }
        public int TotalBookings { get; set; }
        public int TotalParticipants { get; set; }
    }
}