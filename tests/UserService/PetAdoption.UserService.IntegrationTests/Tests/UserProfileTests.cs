using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class UserProfileTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public UserProfileTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
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
        DateTime? LastLoginAt);

    private record UserPreferencesDto(
        string? PreferredPetType,
        List<string>? PreferredSizes,
        string? PreferredAgeRange,
        bool ReceiveEmailNotifications,
        bool ReceiveSmsNotifications);

    private record UpdateProfileResponseDto(bool Success, string Message);
}
