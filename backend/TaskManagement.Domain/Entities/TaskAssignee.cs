namespace TaskManagement.Domain.Entities;

public class TaskAssignee
{
    public int TaskItemId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
