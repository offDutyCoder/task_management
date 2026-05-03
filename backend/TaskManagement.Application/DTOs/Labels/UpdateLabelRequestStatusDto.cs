using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.DTOs.Labels;

public class UpdateLabelRequestStatusDto
{
    [Required]
    public LabelRequestStatus Status { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }
}
