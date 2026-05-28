using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task CreateNotificationAsync(int userId, string title, string message, int? relatedBookingId = null);
        Task NotifyBookingStatusChangedAsync(int bookingId, string oldStatus, string newStatus);
    }
}