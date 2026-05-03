namespace TaskManagement.Domain.Entities;

public class TaskShare
{
    public int TaskItemId { get; set; }
    public int UserId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
