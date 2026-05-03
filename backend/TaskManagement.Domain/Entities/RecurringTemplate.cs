using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class RecurringTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public RecurringFrequency Frequency { get; set; }
    public string? WeekDays { get; set; }
    public int? DayOfMonth { get; set; }
    public bool ExcludeWeekends { get; set; }
    public bool ExcludeHolidays { get; set; }
    public TimeSpan GenerationTime { get; set; }
    public int DefaultStatusId { get; set; }
    public bool IsActive { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TaskStatus DefaultStatus { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<RecurringTemplateAssignee> Assignees { get; set; } = [];
    public ICollection<RecurringTemplateLabel> Labels { get; set; } = [];
    public ICollection<RecurringTemplateShare> Shares { get; set; } = [];
}
