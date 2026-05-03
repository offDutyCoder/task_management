using Moq;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockNotificationService
            .Setup(n => n.NotifyAssignedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
            .Returns(Task.CompletedTask);
        _sut = new TaskService(_mockTaskRepository.Object, _mockUserRepository.Object, _mockNotificationService.Object);
    }

    private static TaskItem BuildTask(int id, int createdByUserId, int? parentTaskId = null, List<TaskShare>? shares = null) => new()
    {
        Id = id,
        Title = "テストタスク",
        StatusId = 1,
        Status = new Domain.Entities.TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E" },
        Priority = TaskPriority.Medium,
        CreatedByUserId = createdByUserId,
        ParentTaskId = parentTaskId,
        Assignees = [],
        TaskLabels = [],
        SubTasks = [],
        Shares = shares ?? [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static User BuildUser(int id, bool isActive = true) => new()
    {
        Id = id,
        LoginId = $"user{id}",
        DisplayName = $"ユーザー{id}",
        PasswordHash = "hash",
        Role = UserRole.Member,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetTaskByIdAsync_WhenUserIsCreator_ShouldReturnTask()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, true)).ReturnsAsync(task);

        // When
        var result = await _sut.GetTaskByIdAsync(1, currentUserId: 10);

        // Then
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenUserInShareList_ShouldReturnTask()
    {
        // Given
        var shares = new List<TaskShare> { new() { TaskItemId = 1, UserId = 20 } };
        var task = BuildTask(id: 1, createdByUserId: 10, shares: shares);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, true)).ReturnsAsync(task);

        // When
        var result = await _sut.GetTaskByIdAsync(1, currentUserId: 20);

        // Then
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenUserNotInShareList_ShouldThrowForbiddenException()
    {
        // Given
        var shares = new List<TaskShare> { new() { TaskItemId = 1, UserId = 10 } };
        var task = BuildTask(id: 1, createdByUserId: 10, shares: shares);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, true)).ReturnsAsync(task);

        // When / Then
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetTaskByIdAsync(1, currentUserId: 99));
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenSubTask_ShouldCheckParentShares()
    {
        // Given
        var parentShares = new List<TaskShare> { new() { TaskItemId = 5, UserId = 20 } };
        var subTask = BuildTask(id: 2, createdByUserId: 10, parentTaskId: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(2, true)).ReturnsAsync(subTask);
        _mockTaskRepository.Setup(r => r.GetParentSharesAsync(5)).ReturnsAsync(parentShares);

        // When (user 20 is in parent shares but not creator)
        var result = await _sut.GetTaskByIdAsync(2, currentUserId: 20);

        // Then
        Assert.Equal(2, result.Id);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenSubTaskAndUserNotInParentShares_ShouldThrowForbiddenException()
    {
        // Given
        var parentShares = new List<TaskShare> { new() { TaskItemId = 5, UserId = 10 } };
        var subTask = BuildTask(id: 2, createdByUserId: 10, parentTaskId: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(2, true)).ReturnsAsync(subTask);
        _mockTaskRepository.Setup(r => r.GetParentSharesAsync(5)).ReturnsAsync(parentShares);

        // When / Then
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetTaskByIdAsync(2, currentUserId: 99));
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, true)).ReturnsAsync((TaskItem?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetTaskByIdAsync(999, currentUserId: 1));
    }

    [Fact]
    public async Task CreateTaskAsync_WhenParentTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((TaskItem?)null);

        var request = new CreateTaskRequest
        {
            Title = "サブタスク",
            StatusId = 1,
            ParentTaskId = 999,
        };

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateTaskAsync(request, currentUserId: 1));
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenUserNotCreator_ShouldThrowForbiddenException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);

        // When / Then
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteTaskAsync(1, currentUserId: 99));
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenUserIsCreator_ShouldCallDeleteAsync()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // When
        await _sut.DeleteTaskAsync(1, currentUserId: 10);

        // Then
        _mockTaskRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task AddAssigneeAsync_WhenTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((TaskItem?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddAssigneeAsync(999, 1, currentUserId: 1));
    }

    [Fact]
    public async Task AddAssigneeAsync_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddAssigneeAsync(1, 99, currentUserId: 10));
    }

    [Fact]
    public async Task AddAssigneeAsync_WhenUserInactive_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var inactiveUser = BuildUser(id: 5, isActive: false);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(inactiveUser);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddAssigneeAsync(1, 5, currentUserId: 10));
    }

    [Fact]
    public async Task AddAssigneeAsync_WhenAlreadyAssigned_ShouldThrowConflictException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var user = BuildUser(id: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockTaskRepository.Setup(r => r.IsAssigneeAsync(1, 5)).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ConflictException>(() => _sut.AddAssigneeAsync(1, 5, currentUserId: 10));
    }

    [Fact]
    public async Task AddAssigneeAsync_WhenValid_ShouldReturnAssigneeDto()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var user = BuildUser(id: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockTaskRepository.Setup(r => r.IsAssigneeAsync(1, 5)).ReturnsAsync(false);
        _mockTaskRepository.Setup(r => r.AddAssigneeAsync(1, 5)).Returns(Task.CompletedTask);

        // When
        var result = await _sut.AddAssigneeAsync(1, 5, currentUserId: 10);

        // Then
        Assert.Equal(5, result.Id);
        Assert.Equal("ユーザー5", result.DisplayName);
        _mockTaskRepository.Verify(r => r.AddAssigneeAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task RemoveAssigneeAsync_WhenTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((TaskItem?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveAssigneeAsync(999, 1, currentUserId: 1));
    }

    [Fact]
    public async Task RemoveAssigneeAsync_WhenNotAssigned_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.IsAssigneeAsync(1, 99)).ReturnsAsync(false);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveAssigneeAsync(1, 99, currentUserId: 10));
    }

    [Fact]
    public async Task RemoveAssigneeAsync_WhenValid_ShouldCallRemoveAsync()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.IsAssigneeAsync(1, 5)).ReturnsAsync(true);
        _mockTaskRepository.Setup(r => r.RemoveAssigneeAsync(1, 5)).Returns(Task.CompletedTask);

        // When
        await _sut.RemoveAssigneeAsync(1, 5, currentUserId: 10);

        // Then
        _mockTaskRepository.Verify(r => r.RemoveAssigneeAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task AddShareUserAsync_WhenTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((TaskItem?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddShareUserAsync(999, 1, currentUserId: 1));
    }

    [Fact]
    public async Task AddShareUserAsync_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddShareUserAsync(1, 99, currentUserId: 10));
    }

    [Fact]
    public async Task RemoveShareUserAsync_WhenTaskNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTaskRepository.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((TaskItem?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveShareUserAsync(999, 1, currentUserId: 1));
    }

    [Fact]
    public async Task AddShareUserAsync_WhenUserHasAccess_ShouldAddShareAndReturnDto()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var user = BuildUser(id: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockTaskRepository.Setup(r => r.IsShareUserAsync(1, 5)).ReturnsAsync(false);
        _mockTaskRepository.Setup(r => r.AddShareUserAsync(1, 5)).Returns(Task.CompletedTask);

        // When
        var result = await _sut.AddShareUserAsync(1, 5, currentUserId: 10);

        // Then
        Assert.Equal(5, result.Id);
        Assert.Equal("ユーザー5", result.DisplayName);
        _mockTaskRepository.Verify(r => r.AddShareUserAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task AddShareUserAsync_WhenUserAlreadyShared_ShouldThrowConflictException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var user = BuildUser(id: 5);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockTaskRepository.Setup(r => r.IsShareUserAsync(1, 5)).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ConflictException>(() => _sut.AddShareUserAsync(1, 5, currentUserId: 10));
    }

    [Fact]
    public async Task AddShareUserAsync_WhenTargetUserIsInactive_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        var inactiveUser = BuildUser(id: 5, isActive: false);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockUserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(inactiveUser);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddShareUserAsync(1, 5, currentUserId: 10));
    }

    [Fact]
    public async Task RemoveShareUserAsync_WhenRemovingCreator_ShouldThrowForbiddenException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);

        // When / Then (userId=10 is the creator)
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.RemoveShareUserAsync(1, 10, currentUserId: 10));
    }

    [Fact]
    public async Task RemoveShareUserAsync_WhenUserNotInShareList_ShouldThrowNotFoundException()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.IsShareUserAsync(1, 99)).ReturnsAsync(false);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveShareUserAsync(1, 99, currentUserId: 10));
    }

    [Fact]
    public async Task RemoveShareUserAsync_WhenValidUser_ShouldRemoveShare()
    {
        // Given
        var task = BuildTask(id: 1, createdByUserId: 10);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.IsShareUserAsync(1, 5)).ReturnsAsync(true);
        _mockTaskRepository.Setup(r => r.RemoveShareUserAsync(1, 5)).Returns(Task.CompletedTask);

        // When
        await _sut.RemoveShareUserAsync(1, 5, currentUserId: 10);

        // Then
        _mockTaskRepository.Verify(r => r.RemoveShareUserAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenNewAssigneeAdded_ShouldNotifyNewAssigneeOnly()
    {
        // Given - task already has assignee userId=5
        var existingAssignee = new TaskAssignee { TaskItemId = 1, UserId = 5 };
        var task = BuildTask(id: 1, createdByUserId: 10);
        task.Assignees = [existingAssignee];

        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.UpdateAsync(
            It.IsAny<TaskItem>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>()))
            .ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, true)).ReturnsAsync(task);

        IEnumerable<int>? capturedNewAssignees = null;
        _mockNotificationService
            .Setup(n => n.NotifyAssignedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
            .Callback<int, string, IEnumerable<int>>((_, _, ids) => capturedNewAssignees = ids)
            .Returns(Task.CompletedTask);

        var request = new UpdateTaskRequest
        {
            Title = "テストタスク",
            StatusId = 1,
            AssigneeIds = [5, 6],  // 5 is existing, 6 is new
            ShareUserIds = [10],
        };

        // When
        await _sut.UpdateTaskAsync(1, request, currentUserId: 10);

        // Then - notification sent only for new assignee (userId=6)
        _mockNotificationService.Verify(n =>
            n.NotifyAssignedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()),
            Times.Once);
        Assert.NotNull(capturedNewAssignees);
        Assert.Equal([6], capturedNewAssignees);
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenNoNewAssignees_ShouldNotSendNotification()
    {
        // Given - task already has assignees userId=5 and 6
        var existingAssignees = new List<TaskAssignee>
        {
            new() { TaskItemId = 1, UserId = 5 },
            new() { TaskItemId = 1, UserId = 6 },
        };
        var task = BuildTask(id: 1, createdByUserId: 10);
        task.Assignees = existingAssignees;

        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, false)).ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.UpdateAsync(
            It.IsAny<TaskItem>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>(),
            It.IsAny<List<int>>()))
            .ReturnsAsync(task);
        _mockTaskRepository.Setup(r => r.GetByIdAsync(1, true)).ReturnsAsync(task);

        var request = new UpdateTaskRequest
        {
            Title = "テストタスク",
            StatusId = 1,
            AssigneeIds = [5, 6],  // same as existing - no new assignees
            ShareUserIds = [10],
        };

        // When
        await _sut.UpdateTaskAsync(1, request, currentUserId: 10);

        // Then - no notification
        _mockNotificationService.Verify(n =>
            n.NotifyAssignedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()),
            Times.Never);
    }
}
