using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Labels;

public class UpdateLabelRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(7)]
    public string Color { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
