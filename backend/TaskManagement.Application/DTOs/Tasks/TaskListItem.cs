using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public class TaskListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public StatusDto Status { get; set; } = null!;
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public List<AssigneeDto> Assignees { get; set; } = [];
    public List<LabelDto> Labels { get; set; } = [];
    public int SubTaskCount { get; set; }
    public bool IsRecurring { get; set; }
}
