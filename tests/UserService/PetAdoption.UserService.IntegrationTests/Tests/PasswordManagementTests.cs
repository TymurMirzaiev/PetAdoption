using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PasswordManagementTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PasswordManagementTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/users/me/change-password
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var email = $"pw-valid-{Guid.NewGuid():N}@test.com";
        var originalPassword = "StrongPass123!";
        var newPassword = "NewStrongPass456!";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, originalPassword);

        var request = new ChangePasswordRequestDto(originalPassword, newPassword);

        // Act
        var response = await client.PostAsJsonAsync("/api/users/me/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var email = $"pw-wrong-{Guid.NewGuid():N}@test.com";
        var originalPassword = "StrongPass123!";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, originalPassword);

        var request = new ChangePasswordRequestDto("WrongPassword123!", "NewStrongPass456!");

        // Act
        var response = await client.PostAsJsonAsync("/api/users/me/change-password", request);

        // Assert - InvalidCredentialsException maps to Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_ReturnsBadRequest()
    {
        // Arrange
        var email = $"pw-short-{Guid.NewGuid():N}@test.com";
        var originalPassword = "StrongPass123!";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, originalPassword);

        var request = new ChangePasswordRequestDto(originalPassword, "Short1!"); // Less than 8 chars

        // Act
        var response = await client.PostAsJsonAsync("/api/users/me/change-password", request);

        // Assert - ArgumentException from Password.ValidatePlainText maps to BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_CanLoginWithNewPassword()
    {
        // Arrange
        var email = $"pw-login-{Guid.NewGuid():N}@test.com";
        var originalPassword = "StrongPass123!";
        var newPassword = "NewStrongPass456!";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, originalPassword);

        var changeRequest = new ChangePasswordRequestDto(originalPassword, newPassword);
        var changeResponse = await client.PostAsJsonAsync("/api/users/me/change-password", changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - login with the new password using a fresh client
        var freshClient = _factory.CreateClient();
        var loginResponse = await freshClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = newPassword
        });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_WhenNotAuthenticated_Returns401()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();
        var request = new ChangePasswordRequestDto("OldPass123!", "NewPass456!");

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync(
            "/api/users/me/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

    private record ChangePasswordResponseDto(bool Success, string Message);

    private record LoginResponseDto(
        bool Success,
        string Token,
        string UserId,
        string Email,
        string FullName,
        string Role,
        int ExpiresIn);
}
