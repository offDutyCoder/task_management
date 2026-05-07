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

public class TasksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TasksControllerTests(WebApplicationFactory<Program> factory)
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

    private static User CreateUser(string loginId, int id = 0, UserRole role = UserRole.Member)
    {
        var user = new User
        {
            LoginId = loginId,
            DisplayName = loginId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 4),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        if (id > 0) user.Id = id;
        return user;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    [Fact]
    public async Task GET_Tasks_WithoutAuth_ShouldReturn401()
    {
        // Given
        var (client, _) = CreateClientWithSeedData();

        // When
        var response = await client.GetAsync("/api/tasks");

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_Task_WithValidData_ShouldReturn201()
    {
        // Given
        User? seededUser = null;
        var (client, _) = CreateClientWithSeedData(db =>
        {
            seededUser = CreateUser("user1");
            db.Users.Add(seededUser);
            db.SaveChanges();
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "user1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        var request = new
        {
            title = "新規タスク",
            statusId = 1,
            priority = 1,
            assigneeIds = new[] { seededUser!.Id },
            labelIds = Array.Empty<int>(),
            shareUserIds = Array.Empty<int>(),
        };

        // When
        var response = await client.PostAsync("/api/tasks", Json(request));

        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("新規タスク", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GET_Task_WhenUserNotInShareList_ShouldReturn403()
    {
        // Given
        const int ownerId = 10;
        const int otherId = 20;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(
                CreateUser("owner1", id: ownerId),
                CreateUser("other1", id: otherId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "プライベートタスク",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "other1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.GetAsync($"/api/tasks/{taskId}");

        // Then
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Task_WhenUserIsNotCreator_ShouldReturn403()
    {
        // Given
        const int ownerId = 10;
        const int otherId = 20;
        const int taskId = 100;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(
                CreateUser("creator1", id: ownerId),
                CreateUser("other2", id: otherId));

            db.TaskItems.Add(new TaskItem
            {
                Id = taskId,
                Title = "削除テスト",
                StatusId = 1,
                Priority = TaskPriority.Medium,
                IsRecurring = false,
                CreatedByUserId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = ownerId });
            db.TaskShares.Add(new TaskShare { TaskItemId = taskId, UserId = otherId });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "other2", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.DeleteAsync($"/api/tasks/{taskId}");

        // Then
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
