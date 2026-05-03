using Moq;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _sut = new UserService(_mockUserRepository.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialsValid_ShouldReturnUser()
    {
        // Given
        var password = "correct-password";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = new User { Id = 1, LoginId = "testuser", PasswordHash = hash, IsActive = true };

        _mockUserRepository.Setup(r => r.GetByLoginIdAsync("testuser")).ReturnsAsync(user);

        // When
        var result = await _sut.AuthenticateAsync(new LoginRequest { LoginId = "testuser", Password = password });

        // Then
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordWrong_ShouldReturnNull()
    {
        // Given
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password", workFactor: 4);
        var user = new User { Id = 1, LoginId = "testuser", PasswordHash = hash, IsActive = true };

        _mockUserRepository.Setup(r => r.GetByLoginIdAsync("testuser")).ReturnsAsync(user);

        // When
        var result = await _sut.AuthenticateAsync(new LoginRequest { LoginId = "testuser", Password = "wrong-password" });

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserInactive_ShouldReturnNull()
    {
        // Given
        var password = "correct-password";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = new User { Id = 1, LoginId = "testuser", PasswordHash = hash, IsActive = false };

        _mockUserRepository.Setup(r => r.GetByLoginIdAsync("testuser")).ReturnsAsync(user);

        // When
        var result = await _sut.AuthenticateAsync(new LoginRequest { LoginId = "testuser", Password = password });

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Given
        _mockUserRepository.Setup(r => r.GetByLoginIdAsync("unknown")).ReturnsAsync((User?)null);

        // When
        var result = await _sut.AuthenticateAsync(new LoginRequest { LoginId = "unknown", Password = "any" });

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_WhenLoginIdExists_ShouldThrowValidationException()
    {
        // Given
        _mockUserRepository.Setup(r => r.ExistsLoginIdAsync("existing", It.IsAny<int?>())).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateUserAsync(new CreateUserRequest
            {
                LoginId = "existing",
                Password = "password123",
                DisplayName = "テストユーザー",
                Role = UserRole.Member,
            }));
    }

    [Fact]
    public async Task CreateUserAsync_WhenLoginIdIsNew_ShouldCreateUser()
    {
        // Given
        var request = new CreateUserRequest
        {
            LoginId = "newuser",
            Password = "password123",
            DisplayName = "新規ユーザー",
            Role = UserRole.Member,
        };

        _mockUserRepository.Setup(r => r.ExistsLoginIdAsync("newuser", It.IsAny<int?>())).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) =>
            {
                u.Id = 1;
                return u;
            });

        // When
        var result = await _sut.CreateUserAsync(request);

        // Then
        Assert.Equal("newuser", result.LoginId);
        Assert.Equal("新規ユーザー", result.DisplayName);
        Assert.Equal(UserRole.Member, result.Role);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordWrong_ShouldThrowValidationException()
    {
        // Given
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password", workFactor: 4);
        var user = new User { Id = 1, PasswordHash = hash };

        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ChangePasswordAsync(1, new ChangePasswordRequest
            {
                CurrentPassword = "wrong-password",
                NewPassword = "new-password123",
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordCorrect_ShouldUpdatePassword()
    {
        // Given
        var currentPassword = "current-password";
        var hash = BCrypt.Net.BCrypt.HashPassword(currentPassword, workFactor: 4);
        var user = new User { Id = 1, PasswordHash = hash };

        User? capturedUser = null;
        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync((User u) => u);

        // When (no exception = success)
        await _sut.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = "new-password123",
        });

        // Then
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        Assert.NotNull(capturedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password123", capturedUser!.PasswordHash));
    }
}
