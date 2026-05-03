using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Auth;

public class LoginResponse
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
