using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IUserService
{
    Task<User?> AuthenticateAsync(LoginRequest request);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<UserResponse> GetUserByIdAsync(int id);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<UserResponse> UpdateMyProfileAsync(int currentUserId, UpdateMyProfileRequest request);
    Task ChangePasswordAsync(int currentUserId, ChangePasswordRequest request);
}
