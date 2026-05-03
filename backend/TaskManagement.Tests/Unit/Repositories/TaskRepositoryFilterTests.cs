using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using Xunit;

namespace TaskManagement.Tests.Unit.Repositories;

public class TaskRepositoryFilterTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedBaseData(AppDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 1,
            LoginId = "user1",
            DisplayName = "ユーザー1",
            PasswordHash = "hash",
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        db.TaskStatuses.Add(new Domain.Entities.TaskStatus
        {
            Id = 1,
            Name = "未着手",
            Color = "#9E9E9E",
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    private static TaskItem BuildTask(int id, DateTime? dueDate, int createdByUserId = 1) => new()
    {
        Id = id,
        Title = $"タスク{id}",
        StatusId = 1,
        Priority = TaskPriority.Medium,
        DueDate = dueDate,
        CreatedByUserId = createdByUserId,
        IsRecurring = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetListAsync_WithDueBefore_ShouldReturnOnlyTasksOnOrBeforeDate()
    {
        using var db = CreateContext();
        SeedBaseData(db);

        db.TaskItems.AddRange(
            BuildTask(1, new DateTime(2026, 5, 1)),
            BuildTask(2, new DateTime(2026, 5, 10)),
            BuildTask(3, new DateTime(2026, 5, 20))
        );
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var repo = new TaskRepository(db);
        var filter = new TaskFilterQuery { DueBefore = new DateTime(2026, 5, 10) };

        var (items, totalCount) = await repo.GetListAsync(filter, currentUserId: 1);

        Assert.Equal(2, totalCount);
        Assert.All(items, t => Assert.True(t.DueDate!.Value <= new DateTime(2026, 5, 10)));
    }

    [Fact]
    public async Task GetListAsync_WithDueAfter_ShouldReturnOnlyTasksOnOrAfterDate()
    {
        using var db = CreateContext();
        SeedBaseData(db);

        db.TaskItems.AddRange(
            BuildTask(1, new DateTime(2026, 5, 1)),
            BuildTask(2, new DateTime(2026, 5, 10)),
            BuildTask(3, new DateTime(2026, 5, 20))
        );
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var repo = new TaskRepository(db);
        var filter = new TaskFilterQuery { DueAfter = new DateTime(2026, 5, 10) };

        var (items, totalCount) = await repo.GetListAsync(filter, currentUserId: 1);

        Assert.Equal(2, totalCount);
        Assert.All(items, t => Assert.True(t.DueDate!.Value >= new DateTime(2026, 5, 10)));
    }

    [Fact]
    public async Task GetListAsync_WithDueDateRange_ShouldReturnTasksInRange()
    {
        using var db = CreateContext();
        SeedBaseData(db);

        db.TaskItems.AddRange(
            BuildTask(1, new DateTime(2026, 5, 1)),
            BuildTask(2, new DateTime(2026, 5, 10)),
            BuildTask(3, new DateTime(2026, 5, 20)),
            BuildTask(4, new DateTime(2026, 5, 31))
        );
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var repo = new TaskRepository(db);
        var filter = new TaskFilterQuery
        {
            DueAfter = new DateTime(2026, 5, 5),
            DueBefore = new DateTime(2026, 5, 25),
        };

        var (items, totalCount) = await repo.GetListAsync(filter, currentUserId: 1);

        Assert.Equal(2, totalCount);
        Assert.All(items, t =>
        {
            Assert.True(t.DueDate!.Value >= new DateTime(2026, 5, 5));
            Assert.True(t.DueDate!.Value <= new DateTime(2026, 5, 25));
        });
    }

    [Fact]
    public async Task GetListAsync_WithDueBefore_WhenTaskHasNullDueDate_ShouldExcludeTask()
    {
        using var db = CreateContext();
        SeedBaseData(db);

        db.TaskItems.AddRange(
            BuildTask(1, new DateTime(2026, 5, 5)),
            BuildTask(2, null)
        );
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var repo = new TaskRepository(db);
        var filter = new TaskFilterQuery { DueBefore = new DateTime(2026, 5, 31) };

        var (items, totalCount) = await repo.GetListAsync(filter, currentUserId: 1);

        Assert.Equal(1, totalCount);
        Assert.All(items, t => Assert.NotNull(t.DueDate));
    }
}
