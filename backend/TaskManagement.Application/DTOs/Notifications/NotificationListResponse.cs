namespace TaskManagement.Application.DTOs.Notifications;

public class NotificationListResponse
{
    public List<NotificationResponse> Items { get; set; } = [];
    public int UnreadCount { get; set; }
}
