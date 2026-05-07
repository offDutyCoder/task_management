using Moq;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockTeamRepository
            .Setup(r => r.GetTeamIdsForUserAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int>());
        _sut = new DashboardService(_mockTaskRepository.Object, _mockTeamRepository.Object);
    }

    private static TaskItem BuildTask(int id, int createdByUserId, List<TaskAssignee>? assignees = null) => new()
    {
        Id = id,
        Title = $"タスク{id}",
        StatusId = 1,
        Status = new Domain.Entities.TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E" },
        Priority = TaskPriority.Medium,
        IsRecurring = false,
        CreatedByUserId = createdByUserId,
        Assignees = assignees ?? [],
        TaskLabels = [],
        SubTasks = [],
        Shares = [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnMyTasksFilteredByAssignee()
    {
        // Given
        const int currentUserId = 10;
        var myTask = BuildTask(id: 1, createdByUserId: 5,
            assignees: [new TaskAssignee { UserId = currentUserId, User = new User { Id = currentUserId, DisplayName = "テストユーザー" }, AssignedAt = DateTime.UtcNow }]);
        var otherTask = BuildTask(id: 2, createdByUserId: 5,
            assignees: [new TaskAssignee { UserId = 99, User = new User { Id = 99, DisplayName = "別ユーザー" }, AssignedAt = DateTime.UtcNow }]);

        _mockTaskRepository
            .Setup(r => r.GetListAsync(It.IsAny<TaskFilterQuery>(), currentUserId))
            .ReturnsAsync(([myTask, otherTask], 2));

        // When
        var result = await _sut.GetDashboardAsync(currentUserId);

        // Then
        Assert.Single(result.MyTasks);
        Assert.Equal(1, result.MyTasks[0].Id);
        Assert.Equal(2, result.TeamTaskGroups.Count);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenIncludeRecurringFalse_ShouldPassIsRecurringFalseToRepository()
    {
        // Given
        const int currentUserId = 10;

        _mockTaskRepository
            .Setup(r => r.GetListAsync(It.IsAny<TaskFilterQuery>(), currentUserId))
            .ReturnsAsync(([], 0));

        // When
        await _sut.GetDashboardAsync(currentUserId, includeRecurring: false);

        // Then
        _mockTaskRepository.Verify(r => r.GetListAsync(
            It.Is<TaskFilterQuery>(f => f.IsRecurring == false),
            currentUserId), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenIncludeRecurringTrue_ShouldPassIsRecurringNullToRepository()
    {
        // Given
        const int currentUserId = 10;

        _mockTaskRepository
            .Setup(r => r.GetListAsync(It.IsAny<TaskFilterQuery>(), currentUserId))
            .ReturnsAsync(([], 0));

        // When
        await _sut.GetDashboardAsync(currentUserId, includeRecurring: true);

        // Then
        _mockTaskRepository.Verify(r => r.GetListAsync(
            It.Is<TaskFilterQuery>(f => f.IsRecurring == null),
            currentUserId), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenTaskAssignedToTeam_ShouldIncludeInMyTasksForTeamMember()
    {
        // Given
        const int currentUserId = 10;
        const int teamId = 1;
        var teamTask = BuildTask(id: 3, createdByUserId: 5);
        teamTask.AssigneeTeamId = teamId;
        teamTask.AssigneeTeam = new Team { Id = teamId, Name = "開発チーム" };

        _mockTeamRepository
            .Setup(r => r.GetTeamIdsForUserAsync(currentUserId))
            .ReturnsAsync(new List<int> { teamId });

        _mockTaskRepository
            .Setup(r => r.GetListAsync(It.IsAny<TaskFilterQuery>(), currentUserId))
            .ReturnsAsync(([teamTask], 1));

        // When
        var result = await _sut.GetDashboardAsync(currentUserId);

        // Then
        Assert.Single(result.MyTasks);
        Assert.Single(result.TeamTaskGroups);
        Assert.Equal("team", result.TeamTaskGroups[0].AssigneeType);
        Assert.Equal("開発チーム", result.TeamTaskGroups[0].AssigneeName);
    }
}
