using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.DTOs.Labels;

public class LabelRequestResponse
{
    public int Id { get; set; }
    public string RequestedName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public LabelRequestStatus Status { get; set; }
    public string RequestedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
