namespace TourManagementSystem.DTOs
{
    public class TourScheduleSimpleDto
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public string TourTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AvailableSlots { get; set; }
        public decimal Price { get; set; }
        public string DisplayText => $"{TourTitle} - {StartTime:dd.MM.yyyy HH:mm} - {Price:C} (мест: {AvailableSlots})";
    }
}