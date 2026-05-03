using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TaskManagement.Tests.Integration.TestHelpers;

public static class AuthTestHelper
{
    public static async Task<string?> LoginAndGetCookieAsync(
        HttpClient client,
        string loginId,
        string password)
    {
        var loginData = JsonSerializer.Serialize(new { loginId, password });
        var content = new StringContent(loginData, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/login", content);
        if (!response.IsSuccessStatusCode) return null;

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        return cookies?.FirstOrDefault();
    }

    public static void AddCookieHeader(HttpClient client, string? cookie)
    {
        if (cookie is not null)
            client.DefaultRequestHeaders.Add("Cookie", cookie);
    }
}
