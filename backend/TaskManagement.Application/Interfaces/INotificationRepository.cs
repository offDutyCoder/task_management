using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface INotificationRepository
{
    Task<(IEnumerable<Notification> Items, int UnreadCount)> GetByUserIdAsync(int userId);
    Task CreateRangeAsync(IEnumerable<Notification> notifications);
    Task<Notification?> GetByIdAsync(int id);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> OverdueNotificationExistsAsync(int taskId, int userId, DateTime date);
}
