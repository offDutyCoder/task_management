namespace TaskManagement.Domain.Entities;

public class TaskLabel
{
    public int TaskItemId { get; set; }
    public int LabelId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
