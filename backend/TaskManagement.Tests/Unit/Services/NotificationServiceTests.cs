using Moq;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _mockNotificationRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _mockNotificationRepository = new Mock<INotificationRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _sut = new NotificationService(_mockNotificationRepository.Object, _mockTaskRepository.Object);
    }

    private static Notification BuildNotification(int id, int userId, bool isRead = false) => new()
    {
        Id = id,
        UserId = userId,
        Type = NotificationType.Assigned,
        TaskItemId = 1,
        Message = "テスト通知",
        IsRead = isRead,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetNotificationsAsync_ReturnsOnlyUserNotifications()
    {
        // Given
        var notifications = new List<Notification>
        {
            BuildNotification(1, userId: 10),
            BuildNotification(2, userId: 10, isRead: true),
        };
        _mockNotificationRepository
            .Setup(r => r.GetByUserIdAsync(10))
            .ReturnsAsync((notifications, 1));

        // When
        var result = await _sut.GetNotificationsAsync(10);

        // Then
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.UnreadCount);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenOtherUsersNotification_ThrowsForbiddenException()
    {
        // Given
        var notification = BuildNotification(id: 1, userId: 10);
        _mockNotificationRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(notification);

        // When / Then
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.MarkAsReadAsync(1, currentUserId: 99));
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenOwner_CallsMarkAsRead()
    {
        // Given
        var notification = BuildNotification(id: 1, userId: 10);
        _mockNotificationRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(notification);
        _mockNotificationRepository
            .Setup(r => r.MarkAsReadAsync(1))
            .Returns(Task.CompletedTask);

        // When
        await _sut.MarkAsReadAsync(1, currentUserId: 10);

        // Then
        _mockNotificationRepository.Verify(r => r.MarkAsReadAsync(1), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_ThrowsNotFoundException()
    {
        // Given
        _mockNotificationRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Notification?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.MarkAsReadAsync(999, currentUserId: 1));
    }

    [Fact]
    public async Task NotifyAssignedAsync_CreatesNotificationForEachAssignee()
    {
        // Given
        var capturedNotifications = new List<Notification>();
        _mockNotificationRepository
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Notification>>()))
            .Callback<IEnumerable<Notification>>(n => capturedNotifications.AddRange(n))
            .Returns(Task.CompletedTask);

        // When
        await _sut.NotifyAssignedAsync(taskId: 1, taskTitle: "テストタスク", assigneeIds: [10, 20, 30]);

        // Then
        Assert.Equal(3, capturedNotifications.Count);
        Assert.All(capturedNotifications, n =>
        {
            Assert.Equal(NotificationType.Assigned, n.Type);
            Assert.Equal(1, n.TaskItemId);
            Assert.Contains("テストタスク", n.Message);
        });
        Assert.Equal(10, capturedNotifications[0].UserId);
        Assert.Equal(20, capturedNotifications[1].UserId);
        Assert.Equal(30, capturedNotifications[2].UserId);
    }

    [Fact]
    public async Task NotifyAssignedAsync_WhenNoAssignees_DoesNotCreateNotifications()
    {
        // When
        await _sut.NotifyAssignedAsync(taskId: 1, taskTitle: "テストタスク", assigneeIds: []);

        // Then
        _mockNotificationRepository.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Notification>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateOverdueNotificationsAsync_SkipsDuplicateNotifications()
    {
        // Given
        var assignee = new TaskAssignee { TaskItemId = 1, UserId = 10 };
        var overdueTask = new TaskItem
        {
            Id = 1,
            Title = "期限超過タスク",
            Assignees = [assignee],
            Status = new Domain.Entities.TaskStatus { Id = 5, Name = "進行中", Color = "#2196F3" },
        };

        _mockTaskRepository
            .Setup(r => r.GetOverdueTasksWithAssigneesAsync())
            .ReturnsAsync([overdueTask]);

        _mockNotificationRepository
            .Setup(r => r.OverdueNotificationExistsAsync(1, 10, It.IsAny<DateTime>()))
            .ReturnsAsync(true);  // 既に通知済み

        // When
        await _sut.GenerateOverdueNotificationsAsync();

        // Then - 通知は生成されない
        _mockNotificationRepository.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Notification>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateOverdueNotificationsAsync_CreatesOverdueNotification_WhenNotDuplicate()
    {
        // Given
        var assignee = new TaskAssignee { TaskItemId = 1, UserId = 10 };
        var overdueTask = new TaskItem
        {
            Id = 1,
            Title = "期限超過タスク",
            Assignees = [assignee],
            Status = new Domain.Entities.TaskStatus { Id = 5, Name = "進行中", Color = "#2196F3" },
        };

        _mockTaskRepository
            .Setup(r => r.GetOverdueTasksWithAssigneesAsync())
            .ReturnsAsync([overdueTask]);

        _mockNotificationRepository
            .Setup(r => r.OverdueNotificationExistsAsync(1, 10, It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        var capturedNotifications = new List<Notification>();
        _mockNotificationRepository
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Notification>>()))
            .Callback<IEnumerable<Notification>>(n => capturedNotifications.AddRange(n))
            .Returns(Task.CompletedTask);

        // When
        await _sut.GenerateOverdueNotificationsAsync();

        // Then
        Assert.Single(capturedNotifications);
        Assert.Equal(NotificationType.Overdue, capturedNotifications[0].Type);
        Assert.Equal(10, capturedNotifications[0].UserId);
        Assert.Contains("期限超過タスク", capturedNotifications[0].Message);
    }
}
