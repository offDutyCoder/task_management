using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Users;

public class UserResponse
{
    public int Id { get; set; }
    public string LoginId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
