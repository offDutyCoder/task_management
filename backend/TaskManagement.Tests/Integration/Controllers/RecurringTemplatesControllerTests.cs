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

public class RecurringTemplatesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecurringTemplatesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, WebApplicationFactory<Program> factory) CreateClientWithSeedData(Action<AppDbContext>? seed = null)
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

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        seed?.Invoke(db);
        db.SaveChanges();

        return (client, factory);
    }

    private static StringContent JsonContent(object obj) =>
        new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static (User admin, User member, Domain.Entities.TaskStatus status) CreateTestData(AppDbContext db)
    {
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var now = DateTime.UtcNow;

        var admin = new User
        {
            LoginId = "admin",
            DisplayName = "管理者",
            PasswordHash = hash,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var member = new User
        {
            LoginId = "member",
            DisplayName = "メンバー",
            PasswordHash = hash,
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var status = new Domain.Entities.TaskStatus
        {
            Name = "未着手",
            Color = "#9E9E9E",
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Users.Add(admin);
        db.Users.Add(member);
        db.TaskStatuses.Add(status);
        return (admin, member, status);
    }

    [Fact]
    public async Task GET_RecurringTemplates_WithoutAuth_ShouldReturn401()
    {
        // Given
        var (client, _) = CreateClientWithSeedData();

        // When
        var response = await client.GetAsync("/api/recurring-templates");

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecurringTemplates_AsMember_ShouldReturn403()
    {
        // Given
        var (client, _) = CreateClientWithSeedData(db => CreateTestData(db));
        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "member", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.GetAsync("/api/recurring-templates");

        // Then
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecurringTemplates_AsAdmin_ShouldReturn200()
    {
        // Given
        var (client, _) = CreateClientWithSeedData(db => CreateTestData(db));
        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.GetAsync("/api/recurring-templates");

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_RecurringTemplates_AsAdmin_ShouldReturn201()
    {
        // Given
        Domain.Entities.TaskStatus? createdStatus = null;
        var (client, factory) = CreateClientWithSeedData(db =>
        {
            var (_, _, status) = CreateTestData(db);
            db.SaveChanges();
            createdStatus = status;
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        var request = new
        {
            title = "朝のミーティング",
            priority = 1,
            frequency = 0,
            generationTime = "08:00",
            defaultStatusId = createdStatus!.Id,
            excludeWeekends = false,
            assigneeIds = Array.Empty<int>(),
            labelIds = Array.Empty<int>(),
            shareUserIds = Array.Empty<int>(),
        };

        // When
        var response = await client.PostAsync("/api/recurring-templates", JsonContent(request));

        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("朝のミーティング", body);
    }

    [Fact]
    public async Task DELETE_RecurringTemplates_AsAdmin_ShouldReturn204()
    {
        // Given
        int templateId = 0;
        var (client, _) = CreateClientWithSeedData(db =>
        {
            var (admin, _, status) = CreateTestData(db);
            db.SaveChanges();
            var template = new RecurringTemplate
            {
                Title = "削除テスト",
                Priority = TaskPriority.Medium,
                Frequency = RecurringFrequency.Daily,
                GenerationTime = TimeSpan.Parse("09:00"),
                DefaultStatusId = status.Id,
                IsActive = true,
                CreatedByUserId = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.RecurringTemplates.Add(template);
            db.SaveChanges();
            templateId = template.Id;
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", "password123");
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.DeleteAsync($"/api/recurring-templates/{templateId}");

        // Then
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
