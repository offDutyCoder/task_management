using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public class TaskFilterQuery
{
    public int? StatusId { get; set; }
    public int? AssigneeId { get; set; }
    public int? LabelId { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? Search { get; set; }
    public bool? IsRecurring { get; set; }
    public DateTime? DueAfter { get; set; }
    public DateTime? DueBefore { get; set; }
    public int Page { get; set; } = 1;

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }
}
