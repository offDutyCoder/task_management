using System.Net;
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

public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
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

    private static User CreateUser(string loginId, int id) => new()
    {
        Id = id,
        LoginId = loginId,
        DisplayName = loginId,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 4),
        Role = UserRole.Member,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static TaskItem CreateTask(int id, int createdByUserId) => new()
    {
        Id = id,
        Title = "テストタスク",
        StatusId = 1,
        Priority = TaskPriority.Medium,
        CreatedByUserId = createdByUserId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static Notification CreateNotification(int id, int userId, int taskItemId, bool isRead = false) => new()
    {
        Id = id,
        UserId = userId,
        Type = NotificationType.Assigned,
        TaskItemId = taskItemId,
        Message = "テスト通知",
        IsRead = isRead,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GET_Notifications_WithoutAuth_ShouldReturn401()
    {
        // Given
        var (client, _) = CreateClientWithSeedData();

        // When
        var response = await client.GetAsync("/api/notifications");

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Notifications_WithAuth_ShouldReturnOwnNotificationsOnly()
    {
        // Given
        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(CreateUser("user1", 1), CreateUser("user2", 2));
            db.TaskItems.Add(CreateTask(id: 1, createdByUserId: 1));
            db.Notifications.Add(CreateNotification(id: 1, userId: 1, taskItemId: 1));
            db.Notifications.Add(CreateNotification(id: 2, userId: 1, taskItemId: 1, isRead: true));
            db.Notifications.Add(CreateNotification(id: 3, userId: 2, taskItemId: 1));  // 他ユーザー
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "user1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.GetAsync("/api/notifications");

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(2, result.GetProperty("items").GetArrayLength());
        Assert.Equal(1, result.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task PUT_MarkAsRead_WhenOtherUsersNotification_ShouldReturn403()
    {
        // Given
        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.AddRange(CreateUser("user1", 1), CreateUser("user2", 2));
            db.TaskItems.Add(CreateTask(id: 1, createdByUserId: 2));
            db.Notifications.Add(CreateNotification(id: 1, userId: 2, taskItemId: 1));  // user2 の通知
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "user1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When - user1 が user2 の通知を既読にしようとする
        var response = await client.PutAsync("/api/notifications/1/read", null);

        // Then
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PUT_ReadAll_ShouldMarkAllUserNotificationsAsRead()
    {
        // Given
        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(CreateUser("user1", 1));
            db.TaskItems.Add(CreateTask(id: 1, createdByUserId: 1));
            db.Notifications.Add(CreateNotification(id: 1, userId: 1, taskItemId: 1));
            db.Notifications.Add(CreateNotification(id: 2, userId: 1, taskItemId: 1));
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "user1", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.PutAsync("/api/notifications/read-all", null);

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await client.GetAsync("/api/notifications");
        var json = await getResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(0, result.GetProperty("unreadCount").GetInt32());
    }
}
