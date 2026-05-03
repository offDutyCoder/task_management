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
using Xunit;

namespace TaskManagement.Tests.Integration.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            });
        });
    }

    private HttpClient CreateClientWithSeedData(Action<AppDbContext>? seed = null)
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

        return client;
    }

    [Fact]
    public async Task POST_Login_WithValidCredentials_ShouldReturn200AndSetCookie()
    {
        // Given
        var password = "password123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);

        var client = CreateClientWithSeedData(db =>
        {
            db.Users.Add(new User
            {
                LoginId = "admin",
                DisplayName = "管理者",
                PasswordHash = hash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        });

        var content = new StringContent(
            JsonSerializer.Serialize(new { loginId = "admin", password }),
            Encoding.UTF8,
            "application/json");

        // When
        var response = await client.PostAsync("/api/auth/login", content);

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task POST_Login_WithInvalidCredentials_ShouldReturn401()
    {
        // Given
        var client = CreateClientWithSeedData();
        var content = new StringContent(
            JsonSerializer.Serialize(new { loginId = "nonexistent", password = "wrong" }),
            Encoding.UTF8,
            "application/json");

        // When
        var response = await client.PostAsync("/api/auth/login", content);

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Users_WithoutAuth_ShouldReturn401()
    {
        // Given
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // When
        var response = await client.GetAsync("/api/users");

        // Then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
