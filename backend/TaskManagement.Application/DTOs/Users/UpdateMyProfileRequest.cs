using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Users;

public class UpdateMyProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
}
