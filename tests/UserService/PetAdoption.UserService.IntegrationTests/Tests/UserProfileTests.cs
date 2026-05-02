using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class UserProfileTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public UserProfileTests(SqlServerFixture sqlFixture)
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
    // GET /api/users/me (Get Profile)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WhenAuthenticated_ReturnsUserProfile()
    {
        // Arrange
        var email = $"profile-get-{Guid.NewGuid():N}@test.com";
        var fullName = "Profile Test User";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, "StrongPass123!", fullName);

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
        profile.FullName.Should().Be(fullName);
        profile.Status.Should().NotBeNullOrEmpty();
        profile.Role.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProfile_WhenNotAuthenticated_Returns401()
    {
        // Arrange - use a client without auth headers
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // PUT /api/users/me (Update Profile)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_WithNewName_ReturnsSuccess()
    {
        // Arrange
        var email = $"profile-name-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithFullName("Updated Full Name")
            .Build();

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // Verify change persisted via GET
        var getResponse = await client.GetAsync("/api/users/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.FullName.Should().Be("Updated Full Name");
    }

    [Fact]
    public async Task UpdateProfile_WithPhoneNumber_ReturnsSuccess()
    {
        // Arrange
        var email = $"profile-phone-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithPhoneNumber("+1234567890")
            .Build();

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // Verify change persisted via GET
        var getResponse = await client.GetAsync("/api/users/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.PhoneNumber.Should().Be("+1234567890");
    }

    [Fact]
    public async Task UpdateProfile_WithPreferences_ReturnsSuccess()
    {
        // Arrange
        var email = $"profile-prefs-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        var preferences = UserPreferencesBuilder.Default()
            .WithPreferredPetType("Cat")
            .WithPreferredSizes(new List<string> { "Small", "Medium" })
            .WithPreferredAgeRange("Young")
            .WithReceiveEmailNotifications(true)
            .WithReceiveSmsNotifications(true)
            .Build();

        var updateRequest = new UpdateProfileRequestBuilder()
            .WithPreferences(preferences)
            .Build();

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // Verify change persisted via GET
        var getResponse = await client.GetAsync("/api/users/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Preferences.Should().NotBeNull();
        profile.Preferences!.PreferredPetType.Should().Be("Cat");
        profile.Preferences.ReceiveSmsNotifications.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProfile_WhenNotAuthenticated_Returns401()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();
        var updateRequest = UpdateProfileRequestBuilder.Default().Build();

        // Act
        var response = await unauthenticatedClient.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // PUT /api/users/me — Bio
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_WithBio_PersistsAndRoundTrips()
    {
        // Arrange
        var email = $"profile-bio-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithBio("I have a big yard and love dogs.")
            .Build();

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/api/users/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Bio.Should().Be("I have a big yard and love dogs.");
    }

    [Fact]
    public async Task UpdateProfile_WithNullBio_ClearsBio()
    {
        // Arrange
        var email = $"profile-bio-clear-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        await client.PutAsJsonAsync("/api/users/me",
            UpdateProfileRequestBuilder.Default().WithBio("initial bio").Build());

        // Act — clear bio
        var response = await client.PutAsJsonAsync("/api/users/me",
            UpdateProfileRequestBuilder.Default().WithBio(null).Build());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await client.GetAsync("/api/users/me");
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Bio.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfile_BioBeyond1000Chars_ReturnsBadRequest()
    {
        // Arrange
        var email = $"profile-bio-long-{Guid.NewGuid():N}@test.com";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email);

        var updateRequest = new { FullName = "Some Name", Bio = new string('x', 1001) };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // Login JWT — bio claim
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WhenUserHasBio_JwtContainsBioClaim()
    {
        // Arrange
        var email = $"bio-jwt-{Guid.NewGuid():N}@test.com";
        var password = "StrongPass123!";
        var client = await AuthHelper.RegisterAndLoginAsync(_client, email, password);

        await client.PutAsJsonAsync("/api/users/me",
            UpdateProfileRequestBuilder.Default().WithBio("Love animals").Build());

        // Act — login again to get fresh token with bio claim
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login",
            new { Email = email, Password = password });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();

        // Decode JWT and verify bio claim
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);
        var bioClaim = jwt.Claims.FirstOrDefault(c => c.Type == "bio");
        bioClaim.Should().NotBeNull("bio claim should be present when user has a bio");
        bioClaim!.Value.Should().Be("Love animals");
    }

    [Fact]
    public async Task Login_WhenUserHasNoBio_JwtOmitsBioClaim()
    {
        // Arrange — freshly registered user has no bio
        var email = $"no-bio-jwt-{Guid.NewGuid():N}@test.com";
        var password = "StrongPass123!";
        await _client.PostAsJsonAsync("/api/users/register",
            new { Email = email, FullName = "No Bio User", Password = password });

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login",
            new { Email = email, Password = password });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result!.Token);
        var bioClaim = jwt.Claims.FirstOrDefault(c => c.Type == "bio");
        bioClaim.Should().BeNull("bio claim should be omitted when user has no bio");
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record UserProfileDto(
        string Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        string Status,
        string Role,
        UserPreferencesDto? Preferences,
        DateTime RegisteredAt,
        DateTime UpdatedAt,
        DateTime? LastLoginAt,
        string? Bio = null);

    private record UserPreferencesDto(
        string? PreferredPetType,
        List<string>? PreferredSizes,
        string? PreferredAgeRange,
        bool ReceiveEmailNotifications,
        bool ReceiveSmsNotifications);

    private record UpdateProfileResponseDto(bool Success, string Message);

    private record LoginResponseDto(
        bool Success,
        string Token,
        string UserId,
        string Email,
        string FullName,
        string Role,
        int ExpiresIn);
}
