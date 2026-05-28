using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(MapToDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Set<Notification>()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, int? relatedBookingId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedBookingId = relatedBookingId
            };

            _context.Set<Notification>().Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Создано уведомление для пользователя {userId}: {title}");
        }

        public async Task NotifyBookingStatusChangedAsync(int bookingId, string oldStatus, string newStatus)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking?.User == null)
                {
                    // Гостевые бронирования (User = null) не получают уведомления
                    _logger.LogDebug($"Бронирование {bookingId} принадлежит гостю, уведомление не отправлено");
                    return;
                }

                var title = $"Статус бронирования #{booking.BookingNumber} изменён";
                var message = $"Статус вашего бронирования изменён с \"{TranslateStatus(oldStatus)}\" на \"{TranslateStatus(newStatus)}\".";

                // Теперь booking.UserId точно не null, можно смело передавать
                await CreateNotificationAsync(booking.UserId.Value, title, message, bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при создании уведомления для бронирования {bookingId}");
            }
        }

        private string TranslateStatus(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает",
                "Confirmed" => "Подтверждено",
                "Cancelled" => "Отменено",
                "Completed" => "Завершено",
                _ => status
            };
        }

        private NotificationResponseDto MapToDto(Notification notification)
        {
            return new NotificationResponseDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                RelatedBookingId = notification.RelatedBookingId
            };
        }
    }
}