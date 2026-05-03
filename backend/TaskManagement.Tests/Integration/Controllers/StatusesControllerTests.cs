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

public class StatusesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StatusesControllerTests(WebApplicationFactory<Program> factory)
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

        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        seed?.Invoke(db);
        db.SaveChanges();

        return (client, factory);
    }

    private static StringContent JsonContent(object obj) =>
        new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    [Fact]
    public async Task GET_Statuses_WithoutAuth_ShouldReturn401()
    {
        // Given
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // When
        var response = await client.GetAsync("/api/statuses");

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Statuses_AsAdmin_ShouldReturn200WithStatuses()
    {
        // Given
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var now = DateTime.UtcNow;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(new User
            {
                LoginId = "admin",
                DisplayName = "管理者",
                PasswordHash = hash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
            // シードデータ (EnsureCreated で HasData から投入) に未着手が含まれるため追加不要
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", password);
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.GetAsync("/api/statuses");

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("未着手", body);
    }

    [Fact]
    public async Task POST_Statuses_AsAdmin_ShouldReturn201()
    {
        // Given
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var now = DateTime.UtcNow;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(new User
            {
                LoginId = "admin",
                DisplayName = "管理者",
                PasswordHash = hash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", password);
        AuthTestHelper.AddCookieHeader(client, cookie);

        var content = JsonContent(new { name = "テスト", color = "#123456", displayOrder = 10 });

        // When
        var response = await client.PostAsync("/api/statuses", content);

        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task POST_Statuses_AsMember_ShouldReturn403()
    {
        // Given
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var now = DateTime.UtcNow;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(new User
            {
                LoginId = "member",
                DisplayName = "一般ユーザー",
                PasswordHash = hash,
                Role = UserRole.Member,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "member", password);
        AuthTestHelper.AddCookieHeader(client, cookie);

        var content = JsonContent(new { name = "テスト", color = "#123456", displayOrder = 10 });

        // When
        var response = await client.PostAsync("/api/statuses", content);

        // Then
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Statuses_WhenNotFound_ShouldReturn404()
    {
        // Given
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var now = DateTime.UtcNow;

        var (client, _) = CreateClientWithSeedData(db =>
        {
            db.Users.Add(new User
            {
                LoginId = "admin",
                DisplayName = "管理者",
                PasswordHash = hash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        });

        var cookie = await AuthTestHelper.LoginAndGetCookieAsync(client, "admin", password);
        AuthTestHelper.AddCookieHeader(client, cookie);

        // When
        var response = await client.DeleteAsync("/api/statuses/9999");

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
