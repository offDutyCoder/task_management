using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Users;

public class CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Member;
}
