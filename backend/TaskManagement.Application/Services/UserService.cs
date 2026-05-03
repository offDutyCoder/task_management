using BCrypt.Net;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> AuthenticateAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByLoginIdAsync(request.LoginId);
        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ユーザー", id);
        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        if (await _userRepository.ExistsLoginIdAsync(request.LoginId))
            throw new ValidationException($"ログインID '{request.LoginId}' は既に使用されています");

        var user = new User
        {
            LoginId = request.LoginId,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var created = await _userRepository.CreateAsync(user);
        return MapToResponse(created);
    }

    public async Task<UserResponse> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ユーザー", id);

        user.DisplayName = request.DisplayName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(user);
        return MapToResponse(updated);
    }

    public async Task<UserResponse> UpdateMyProfileAsync(int currentUserId, UpdateMyProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new NotFoundException("ユーザー", currentUserId);

        user.DisplayName = request.DisplayName;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(user);
        return MapToResponse(updated);
    }

    public async Task ChangePasswordAsync(int currentUserId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new NotFoundException("ユーザー", currentUserId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("現在のパスワードが正しくありません");

        if (request.CurrentPassword == request.NewPassword)
            throw new ValidationException("新しいパスワードは現在のパスワードと異なるものを設定してください");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        LoginId = user.LoginId,
        DisplayName = user.DisplayName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
    };
}
