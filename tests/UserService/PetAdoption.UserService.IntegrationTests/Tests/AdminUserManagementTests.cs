using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MongoDB.Driver;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class AdminUserManagementTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _regularClient = null!;
    private HttpClient _unauthenticatedClient = null!;
    private string _adminUserId = null!;
    private string _regularUserId = null!;

    private const string AdminEmail = "admin-mgmt@test.com";
    private const string AdminPassword = "AdminPass123!";
    private const string RegularEmail = "regular-mgmt@test.com";
    private const string RegularPassword = "RegularPass123!";

    public AdminUserManagementTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
        _unauthenticatedClient = _factory.CreateClient();

        // 1. Register admin user via API
        var adminRegisterClient = _factory.CreateClient();
        var adminRegisterResponse = await adminRegisterClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = AdminEmail,
            FullName = "Admin User",
            Password = AdminPassword
        });
        adminRegisterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminRegResult = await adminRegisterResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _adminUserId = adminRegResult!.UserId;

        // 2. Promote to admin via direct MongoDB update
        var db = _factory.GetTestDatabase();
        var usersCollection = db.GetCollection<MongoDB.Bson.BsonDocument>("Users");
        var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", _adminUserId);
        var update = Builders<MongoDB.Bson.BsonDocument>.Update.Set("Role", 1); // Admin = 1
        await usersCollection.UpdateOneAsync(filter, update);

        // 3. Login as admin to get admin JWT
        _adminClient = _factory.CreateClient();
        var adminLoginResponse = await _adminClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = AdminEmail,
            Password = AdminPassword
        });
        adminLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminLoginResult = await adminLoginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminLoginResult!.Token);

        // 4. Register regular user via API
        var regularRegisterClient = _factory.CreateClient();
        var regularRegisterResponse = await regularRegisterClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = RegularEmail,
            FullName = "Regular User",
            Password = RegularPassword
        });
        regularRegisterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regularRegResult = await regularRegisterResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _regularUserId = regularRegResult!.UserId;

        // 5. Login as regular user
        _regularClient = _factory.CreateClient();
        var regularLoginResponse = await _regularClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = RegularEmail,
            Password = RegularPassword
        });
        regularLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regularLoginResult = await regularLoginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _regularClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regularLoginResult!.Token);
    }

    public async Task DisposeAsync()
    {
        _adminClient.Dispose();
        _regularClient.Dispose();
        _unauthenticatedClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/users (Admin only - List users)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsPaginatedList()
    {
        // Act
        var response = await _adminClient.GetAsync("/api/users?skip=0&take=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetUsersResponseDto>();
        result.Should().NotBeNull();
        result!.Users.Should().NotBeEmpty();
        result.Total.Should().BeGreaterThanOrEqualTo(2); // At least admin + regular user
        result.Skip.Should().Be(0);
        result.Take.Should().Be(50);
    }

    [Fact]
    public async Task GetUsers_AsRegularUser_Returns403()
    {
        // Act
        var response = await _regularClient.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WhenNotAuthenticated_Returns401()
    {
        // Act
        var response = await _unauthenticatedClient.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/users/{id} (Admin only - Get user by ID)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_AsAdmin_ReturnsUser()
    {
        // Act
        var response = await _adminClient.GetAsync($"/api/users/{_regularUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(_regularUserId);
        user.Email.Should().Be(RegularEmail);
        user.FullName.Should().Be("Regular User");
        user.Role.Should().Be("User");
        user.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetUserById_AsAdmin_NonExistentUser_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _adminClient.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_AsRegularUser_Returns403()
    {
        // Act
        var response = await _regularClient.GetAsync($"/api/users/{_adminUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/users/{id}/suspend (Admin only)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SuspendUser_AsAdmin_ReturnsSuccess()
    {
        // Arrange - create a user to suspend
        var suspendEmail = $"suspend-target-{Guid.NewGuid():N}@test.com";
        var registerClient = _factory.CreateClient();
        var regResponse = await registerClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = suspendEmail,
            FullName = "Suspend Target",
            Password = "SuspendPass123!"
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regResult = await regResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var targetUserId = regResult!.UserId;

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/users/{targetUserId}/suspend",
            new { Reason = "Violation of terms" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SuspendUserResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("suspended");
    }

    [Fact]
    public async Task SuspendUser_AsRegularUser_Returns403()
    {
        // Act
        var response = await _regularClient.PostAsJsonAsync(
            $"/api/users/{_adminUserId}/suspend",
            new { Reason = "Trying to suspend admin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuspendUser_SuspendedUserCannotLogin()
    {
        // Arrange - create and suspend a user
        var suspendEmail = $"suspend-login-{Guid.NewGuid():N}@test.com";
        var suspendPassword = "SuspendLogin123!";
        var registerClient = _factory.CreateClient();
        var regResponse = await registerClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = suspendEmail,
            FullName = "Suspend Login Test",
            Password = suspendPassword
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regResult = await regResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var targetUserId = regResult!.UserId;

        // Suspend the user
        var suspendResponse = await _adminClient.PostAsJsonAsync(
            $"/api/users/{targetUserId}/suspend",
            new { Reason = "Testing suspension" });
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - try to login as suspended user
        var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = suspendEmail,
            Password = suspendPassword
        });

        // Assert - UserSuspendedException maps to Forbidden
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/users/{id}/promote-to-admin (Admin only)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PromoteToAdmin_AsAdmin_ReturnsSuccess()
    {
        // Arrange - create a user to promote
        var promoteEmail = $"promote-target-{Guid.NewGuid():N}@test.com";
        var registerClient = _factory.CreateClient();
        var regResponse = await registerClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = promoteEmail,
            FullName = "Promote Target",
            Password = "PromotePass123!"
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regResult = await regResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var targetUserId = regResult!.UserId;

        // Act
        var response = await _adminClient.PostAsync(
            $"/api/users/{targetUserId}/promote-to-admin",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PromoteToAdminResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("admin");

        // Verify - get user and check role
        var getUserResponse = await _adminClient.GetAsync($"/api/users/{targetUserId}");
        getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await getUserResponse.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task PromoteToAdmin_AsRegularUser_Returns403()
    {
        // Act
        var response = await _regularClient.PostAsync(
            $"/api/users/{_regularUserId}/promote-to-admin",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record RegisterResponse(
        bool Success,
        string UserId,
        string Email,
        string FullName,
        string Role);

    private record LoginResponseDto(
        bool Success,
        string Token,
        string UserId,
        string Email,
        string FullName,
        string Role,
        int ExpiresIn);

    private record GetUsersResponseDto(
        List<UserListItemDto> Users,
        int Total,
        int Skip,
        int Take);

    private record UserListItemDto(
        string Id,
        string Email,
        string FullName,
        string Status,
        string Role,
        DateTime RegisteredAt);

    private record UserDto(
        string Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        string Status,
        string Role,
        object? Preferences,
        DateTime RegisteredAt,
        DateTime UpdatedAt,
        DateTime? LastLoginAt);

    private record SuspendUserResponseDto(bool Success, string Message);

    private record PromoteToAdminResponseDto(bool Success, string Message);
}
