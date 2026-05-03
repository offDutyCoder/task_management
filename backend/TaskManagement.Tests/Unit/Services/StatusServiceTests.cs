using Moq;
using TaskManagement.Application.DTOs.Statuses;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class StatusServiceTests
{
    private readonly Mock<IStatusRepository> _mockStatusRepository;
    private readonly StatusService _sut;

    public StatusServiceTests()
    {
        _mockStatusRepository = new Mock<IStatusRepository>();
        _sut = new StatusService(_mockStatusRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllStatuses()
    {
        // Given
        var statuses = new[]
        {
            new Domain.Entities.TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Domain.Entities.TaskStatus { Id = 2, Name = "進行中", Color = "#2196F3", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        _mockStatusRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(statuses);

        // When
        var result = await _sut.GetAllAsync();

        // Then
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenStatusExists_ShouldReturnStatus()
    {
        // Given
        var status = new Domain.Entities.TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _mockStatusRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(status);

        // When
        var result = await _sut.GetByIdAsync(1);

        // Then
        Assert.Equal(1, result.Id);
        Assert.Equal("未着手", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenStatusNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockStatusRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Domain.Entities.TaskStatus?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WhenNameExists_ShouldThrowValidationException()
    {
        // Given
        _mockStatusRepository
            .Setup(r => r.ExistsNameAsync("既存", It.IsAny<int?>()))
            .ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new CreateStatusRequest { Name = "既存", Color = "#FFFFFF", DisplayOrder = 6 }));
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsNew_ShouldCreateAndReturnStatus()
    {
        // Given
        var request = new CreateStatusRequest { Name = "新ステータス", Color = "#FFFFFF", DisplayOrder = 6 };

        _mockStatusRepository
            .Setup(r => r.ExistsNameAsync("新ステータス", It.IsAny<int?>()))
            .ReturnsAsync(false);
        _mockStatusRepository
            .Setup(r => r.CreateAsync(It.IsAny<Domain.Entities.TaskStatus>()))
            .ReturnsAsync((Domain.Entities.TaskStatus s) =>
            {
                s.Id = 10;
                return s;
            });

        // When
        var result = await _sut.CreateAsync(request);

        // Then
        Assert.Equal(10, result.Id);
        Assert.Equal("新ステータス", result.Name);
        Assert.Equal("#FFFFFF", result.Color);
        Assert.Equal(6, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockStatusRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Domain.Entities.TaskStatus?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(99, new UpdateStatusRequest { Name = "テスト", Color = "#000000", DisplayOrder = 1, IsActive = true }));
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusExists_ShouldUpdateAndReturn()
    {
        // Given
        var status = new Domain.Entities.TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var request = new UpdateStatusRequest { Name = "変更後", Color = "#000000", DisplayOrder = 2, IsActive = false };

        _mockStatusRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(status);
        _mockStatusRepository
            .Setup(r => r.ExistsNameAsync("変更後", 1))
            .ReturnsAsync(false);
        _mockStatusRepository.Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.TaskStatus>()))
            .ReturnsAsync((Domain.Entities.TaskStatus s) => s);

        // When
        var result = await _sut.UpdateAsync(1, request);

        // Then
        Assert.Equal("変更後", result.Name);
        Assert.Equal("#000000", result.Color);
        Assert.Equal(2, result.DisplayOrder);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_WhenStatusNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockStatusRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Domain.Entities.TaskStatus?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenStatusUsedByTask_ShouldThrowConflictException()
    {
        // Given
        var status = new Domain.Entities.TaskStatus { Id = 1, Name = "進行中", Color = "#2196F3", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _mockStatusRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(status);
        _mockStatusRepository.Setup(r => r.ExistsUsedByTaskAsync(1)).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_WhenStatusNotUsed_ShouldSetInactive()
    {
        // Given
        var status = new Domain.Entities.TaskStatus { Id = 1, Name = "未使用", Color = "#FFFFFF", DisplayOrder = 9, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        Domain.Entities.TaskStatus? capturedStatus = null;
        _mockStatusRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(status);
        _mockStatusRepository.Setup(r => r.ExistsUsedByTaskAsync(1)).ReturnsAsync(false);
        _mockStatusRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.TaskStatus>()))
            .Callback<Domain.Entities.TaskStatus>(s => capturedStatus = s)
            .ReturnsAsync((Domain.Entities.TaskStatus s) => s);

        // When
        await _sut.DeleteAsync(1);

        // Then
        _mockStatusRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.TaskStatus>()), Times.Once);
        Assert.NotNull(capturedStatus);
        Assert.False(capturedStatus!.IsActive);
    }
}
