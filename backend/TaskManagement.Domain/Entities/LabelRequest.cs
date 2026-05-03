namespace TaskManagement.Domain.Entities;

public class LabelRequest
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public LabelRequestStatus Status { get; set; } = LabelRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
}

public enum LabelRequestStatus { Pending, Approved, Rejected }
