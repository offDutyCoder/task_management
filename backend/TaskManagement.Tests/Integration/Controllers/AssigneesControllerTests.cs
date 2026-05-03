using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Tests.Integration.TestHelpers;
using Xunit;

namespace TaskManagement.Tests.Integration.Controllers;

public class AssigneesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AssigneesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient Client, WebApplicationFactory<Program> Factory) CreateClientWithSeedData(Action<AppDbContext>? seed = null)
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase(dbName));
            });
        });

        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        db.ChangeTracker.Clear();

        seed?.Invoke(db);
        db.SaveChanges();

        return (client, factory);
    }

    private static User CreateUser(string loginId, int id = 0, UserRole role = UserRole.Member, bool isActive = true)
    {
        var user = new User
        {
            LoginId = loginId,
            DisplayName = loginId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 4),
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        if (id > 0) user.Id = id;
        return user;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    [Fact]
    public async Task POST_Assignee_WhenValid_ShouldReturn201()
    {
        // Given
        const int ownerId = 10;
        const int assigneeUserId = 20;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(
                CreateUser("owner1", id: ownerId),
                CreateUser("member1", id: assigneeUserId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "担当者テストタスク",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "owner1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.PostAsync($"/api/tasks/{taskId}/assignees", Json(new { userId = assigneeUserId }));

        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(assigneeUserId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task POST_Assignee_WhenAlreadyAssigned_ShouldReturn409()
    {
        // Given
        const int ownerId = 10;
        const int assigneeUserId = 20;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(
                CreateUser("owner2", id: ownerId),
                CreateUser("member2", id: assigneeUserId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "担当者テストタスク2",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
            db.TaskAssignees.Add(new TaskAssignee
            {
                TaskItemId = taskId,
                UserId = assigneeUserId,
                AssignedAt = DateTime.UtcNow,
            });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "owner2", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.PostAsync($"/api/tasks/{taskId}/assignees", Json(new { userId = assigneeUserId }));

        // Then
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Assignee_WhenValid_ShouldReturn204()
    {
        // Given
        const int ownerId = 10;
        const int assigneeUserId = 20;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(
                CreateUser("owner3", id: ownerId),
                CreateUser("member3", id: assigneeUserId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "担当者削除テスト",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
            db.TaskAssignees.Add(new TaskAssignee
            {
                TaskItemId = taskId,
                UserId = assigneeUserId,
                AssignedAt = DateTime.UtcNow,
            });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "owner3", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.DeleteAsync($"/api/tasks/{taskId}/assignees/{assigneeUserId}");

        // Then
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Assignee_WhenNotAssigned_ShouldReturn404()
    {
        // Given
        const int ownerId = 10;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(CreateUser("owner4", id: ownerId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "担当者なし削除テスト",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "owner4", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.DeleteAsync($"/api/tasks/{taskId}/assignees/999");

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_Assignee_WithoutAuth_ShouldReturn401()
    {
        // Given
        var (client, _) = CreateClientWithSeedData();

        // When
        var response = await client.PostAsync("/api/tasks/1/assignees", Json(new { userId = 1 }));

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
