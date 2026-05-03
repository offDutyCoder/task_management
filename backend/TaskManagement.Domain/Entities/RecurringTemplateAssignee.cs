namespace TaskManagement.Domain.Entities;

public class RecurringTemplateAssignee
{
    public int RecurringTemplateId { get; set; }
    public int UserId { get; set; }

    public RecurringTemplate Template { get; set; } = null!;
    public User User { get; set; } = null!;
}
