using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.IntegrationTests.Infrastructure;

namespace PetAdoption.UserService.IntegrationTests.Helpers;

internal static class AuthSetupHelper
{
    /// <summary>
    /// Registers a user, promotes them to the given role via direct SQL, and logs in to obtain a JWT.
    /// Returns an authenticated <see cref="HttpClient"/> along with the user's ID.
    /// </summary>
    public static async Task<(HttpClient Client, string UserId)> CreatePromotedUserAsync(
        UserServiceWebAppFactory factory,
        UserRole role,
        string email,
        string fullName,
        string password)
    {
        // Register
        using var registerClient = factory.CreateClient();
        var registerResponse = await registerClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = email,
            FullName = fullName,
            Password = password
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var userId = registerResult!.UserId;

        // Promote via raw SQL
        await using var db = factory.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Users SET Role = {(int)role} WHERE Id = {userId}");

        // Login
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        return (client, userId);
    }
}
