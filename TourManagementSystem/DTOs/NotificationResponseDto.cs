namespace TourManagementSystem.DTOs
{
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedBookingId { get; set; }
    }

    public class NotificationCountDto
    {
        public int UnreadCount { get; set; }
    }
}