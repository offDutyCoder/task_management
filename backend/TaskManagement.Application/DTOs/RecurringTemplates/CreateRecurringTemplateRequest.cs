using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.RecurringTemplates;

public class CreateRecurringTemplateRequest
{
    [Required(ErrorMessage = "タイトルは必須です")]
    [MaxLength(200, ErrorMessage = "タイトルは200文字以内で入力してください")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "説明は4000文字以内で入力してください")]
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; }

    public RecurringFrequency Frequency { get; set; }

    public int[]? WeekDays { get; set; }

    [Range(1, 31, ErrorMessage = "月次日は1〜31の値を入力してください")]
    public int? DayOfMonth { get; set; }

    public bool ExcludeWeekends { get; set; }

    [Required(ErrorMessage = "生成時刻は必須です")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "生成時刻は HH:mm 形式で入力してください")]
    public string GenerationTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "初期ステータスは必須です")]
    public int DefaultStatusId { get; set; }

    public int[] AssigneeIds { get; set; } = [];

    public int[] LabelIds { get; set; } = [];

    public int[] ShareUserIds { get; set; } = [];
}
