using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? ParentTaskId { get; set; }
    public bool IsRecurring { get; set; }
    public int? RecurringTemplateId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int? AssigneeTeamId { get; set; }

    public TaskStatus Status { get; set; } = null!;
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = [];
    public ICollection<TaskAssignee> Assignees { get; set; } = [];
    public ICollection<TaskLabel> TaskLabels { get; set; } = [];
    public ICollection<TaskShare> Shares { get; set; } = [];
    public User CreatedBy { get; set; } = null!;
    public Team? AssigneeTeam { get; set; }
}
