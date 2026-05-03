namespace TaskManagement.Domain.Entities;

public class RecurringTemplateLabel
{
    public int RecurringTemplateId { get; set; }
    public int LabelId { get; set; }

    public RecurringTemplate Template { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
