using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Notifications;

public class NotificationResponse
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public int? TaskItemId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
