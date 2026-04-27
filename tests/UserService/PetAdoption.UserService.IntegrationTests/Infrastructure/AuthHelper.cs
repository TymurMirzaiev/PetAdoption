using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PetAdoption.UserService.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    public static async Task<HttpClient> RegisterAndLoginAsync(
        HttpClient client,
        string email = "testuser@example.com",
        string password = "StrongPass123!",
        string fullName = "Test User")
    {
        // Register
        await client.PostAsJsonAsync("/api/users/register", new
        {
            Email = email,
            FullName = fullName,
            Password = password
        });

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        return client;
    }

    public record LoginResponse(
        bool Success,
        string Token,
        string UserId,
        string Email,
        string FullName,
        string Role,
        int ExpiresIn);
}
