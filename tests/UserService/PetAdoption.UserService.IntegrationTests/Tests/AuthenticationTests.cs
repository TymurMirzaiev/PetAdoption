using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Helpers;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class AuthenticationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthenticationTests(SqlServerFixture sqlFixture)
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
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task RegisterUserAsync(
        string email = "helper@example.com",
        string password = "SecurePass123!",
        string fullName = "Helper User",
        string? phoneNumber = null)
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail(email)
            .WithPassword(password)
            .WithFullName(fullName)
            .WithPhoneNumber(phoneNumber)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);
        response.EnsureSuccessStatusCode();
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/users/register
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("register-valid@example.com")
            .WithFullName("Valid User")
            .WithPassword("SecurePass123!")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = "duplicate@example.com";
        await RegisterUserAsync(email: email);

        var request = RegisterUserRequestBuilder.Default()
            .WithEmail(email)
            .WithFullName("Another User")
            .WithPassword("AnotherPass123!")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("not-an-email")
            .WithFullName("Invalid Email User")
            .WithPassword("SecurePass123!")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("shortpwd@example.com")
            .WithFullName("Short Password User")
            .WithPassword("1234567") // 7 chars, less than 8
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithOptionalPhoneNumber_ReturnsSuccess()
    {
        // Arrange
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("withphone@example.com")
            .WithFullName("Phone User")
            .WithPassword("SecurePass123!")
            .WithPhoneNumber("+1234567890")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_ReturnsBadRequest()
    {
        // Arrange - phone with less than 10 digits
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("badphone@example.com")
            .WithFullName("Bad Phone User")
            .WithPassword("SecurePass123!")
            .WithPhoneNumber("12345") // Less than 10 digits
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortName_ReturnsBadRequest()
    {
        // Arrange - name less than 2 characters
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("shortname@example.com")
            .WithFullName("A") // 1 char, less than 2
            .WithPassword("SecurePass123!")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/users/login
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = "login-valid@example.com";
        var password = "SecurePass123!";
        await RegisterUserAsync(email: email, password: password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = "login-wrongpwd@example.com";
        await RegisterUserAsync(email: email, password: "CorrectPass123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = "WrongPass123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = "nonexistent@example.com",
            Password = "SomePass123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ResponseContainsUserInfo()
    {
        // Arrange
        var email = "login-info@example.com";
        var password = "SecurePass123!";
        var fullName = "Info User";
        await RegisterUserAsync(email: email, password: password, fullName: fullName);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.UserId.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
        result.FullName.Should().Be(fullName);
        result.Role.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record RegisterUserResponse(
        bool Success,
        string UserId,
        string Message);
}
