using TaskManagement.Application.DTOs.Notifications;

namespace TaskManagement.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int currentUserId);
    Task MarkAllAsReadAsync(int userId);
    Task NotifyAssignedAsync(int taskId, string taskTitle, IEnumerable<int> assigneeIds);
    Task GenerateOverdueNotificationsAsync();
}
