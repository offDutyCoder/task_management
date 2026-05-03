using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITaskRepository _taskRepository;

    public NotificationService(INotificationRepository notificationRepository, ITaskRepository taskRepository)
    {
        _notificationRepository = notificationRepository;
        _taskRepository = taskRepository;
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(int userId)
    {
        var (items, unreadCount) = await _notificationRepository.GetByUserIdAsync(userId);

        return new NotificationListResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            UnreadCount = unreadCount,
        };
    }

    public async Task MarkAsReadAsync(int notificationId, int currentUserId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId)
            ?? throw new NotFoundException("通知", notificationId);

        if (notification.UserId != currentUserId)
            throw new ForbiddenException("この通知を操作する権限がありません");

        await _notificationRepository.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task NotifyAssignedAsync(int taskId, string taskTitle, IEnumerable<int> assigneeIds)
    {
        var message = $"「{taskTitle}」の担当者に追加されました";
        var notifications = assigneeIds.Select(userId => new Notification
        {
            UserId = userId,
            Type = NotificationType.Assigned,
            TaskItemId = taskId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });

        await _notificationRepository.CreateRangeAsync(notifications);
    }

    public async Task GenerateOverdueNotificationsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var overdueTasks = await _taskRepository.GetOverdueTasksWithAssigneesAsync();

        var notifications = new List<Notification>();
        foreach (var task in overdueTasks)
        {
            foreach (var assignee in task.Assignees)
            {
                var alreadyNotified = await _notificationRepository
                    .OverdueNotificationExistsAsync(task.Id, assignee.UserId, today);

                if (!alreadyNotified)
                {
                    notifications.Add(new Notification
                    {
                        UserId = assignee.UserId,
                        Type = NotificationType.Overdue,
                        TaskItemId = task.Id,
                        Message = $"「{task.Title}」の期限が超過しています",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }
        }

        if (notifications.Count > 0)
            await _notificationRepository.CreateRangeAsync(notifications);
    }

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        TaskItemId = n.TaskItemId,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
    };
}
