using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.RecurringTemplates;

public class RecurringTemplateResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int[]? WeekDays { get; set; }
    public int? DayOfMonth { get; set; }
    public bool ExcludeWeekends { get; set; }
    public string GenerationTime { get; set; } = string.Empty;
    public int DefaultStatusId { get; set; }
    public bool IsActive { get; set; }
    public AssigneeDto[] Assignees { get; set; } = [];
    public LabelDto[] Labels { get; set; } = [];
    public AssigneeDto[] ShareUsers { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
