using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public class TaskDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StatusDto Status { get; set; } = null!;
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskListItem? ParentTask { get; set; }
    public List<TaskListItem> SubTasks { get; set; } = [];
    public List<AssigneeDto> Assignees { get; set; } = [];
    public List<LabelDto> Labels { get; set; } = [];
    public List<AssigneeDto> ShareUsers { get; set; } = [];
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
