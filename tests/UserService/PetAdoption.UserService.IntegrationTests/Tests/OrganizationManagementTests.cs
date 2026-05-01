using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class OrganizationManagementTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _platformAdminClient = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _regularClient = null!;
    private HttpClient _unauthenticatedClient = null!;
    private string _platformAdminUserId = null!;
    private string _regularUserId = null!;

    private const string PlatformAdminEmail = "platform-admin-org@test.com";
    private const string PlatformAdminPassword = "PlatformPass123!";
    private const string AdminEmail = "admin-org@test.com";
    private const string AdminPassword = "AdminPass123!";
    private const string RegularEmail = "regular-org@test.com";
    private const string RegularPassword = "RegularPass123!";

    public OrganizationManagementTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_sqlFixture.ConnectionString);
        _unauthenticatedClient = _factory.CreateClient();

        // 1. Register platform admin user
        var paRegClient = _factory.CreateClient();
        var paRegResponse = await paRegClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = PlatformAdminEmail,
            FullName = "Platform Admin",
            Password = PlatformAdminPassword
        });
        paRegResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paRegResult = await paRegResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _platformAdminUserId = paRegResult!.UserId;

        // 2. Promote to PlatformAdmin via direct SQL
        await using var db = _factory.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Users SET Role = {(int)UserRole.PlatformAdmin} WHERE Id = {_platformAdminUserId}");

        // 3. Login as platform admin
        _platformAdminClient = _factory.CreateClient();
        var paLoginResponse = await _platformAdminClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = PlatformAdminEmail,
            Password = PlatformAdminPassword
        });
        paLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paLoginResult = await paLoginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _platformAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", paLoginResult!.Token);

        // 4. Register and login as regular admin
        var adminRegClient = _factory.CreateClient();
        var adminRegResponse = await adminRegClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = AdminEmail,
            FullName = "Admin User",
            Password = AdminPassword
        });
        adminRegResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminRegResult = await adminRegResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Users SET Role = {(int)UserRole.Admin} WHERE Id = {adminRegResult!.UserId}");

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

        // 5. Register and login as regular user
        var regClient = _factory.CreateClient();
        var regResponse = await regClient.PostAsJsonAsync("/api/users/register", new
        {
            Email = RegularEmail,
            FullName = "Regular User",
            Password = RegularPassword
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regResult = await regResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _regularUserId = regResult!.UserId;

        _regularClient = _factory.CreateClient();
        var regLoginResponse = await _regularClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = RegularEmail,
            Password = RegularPassword
        });
        regLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regLoginResult = await regLoginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _regularClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regLoginResult!.Token);
    }

    public async Task DisposeAsync()
    {
        _platformAdminClient.Dispose();
        _adminClient.Dispose();
        _regularClient.Dispose();
        _unauthenticatedClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<CreateOrganizationResponseDto> CreateOrgAsync(string? name = null, string? slug = null)
    {
        var uniqueSlug = slug ?? $"org-{Guid.NewGuid():N}"[..30];
        var request = CreateOrganizationRequestBuilder.Default()
            .WithName(name ?? "Test Org")
            .WithSlug(uniqueSlug)
            .Build();

        var response = await _platformAdminClient.PostAsJsonAsync("/api/organizations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreateOrganizationResponseDto>())!;
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/organizations (Create)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrganization_AsPlatformAdmin_ReturnsCreated()
    {
        // Arrange
        var request = CreateOrganizationRequestBuilder.Default()
            .WithName("Happy Paws")
            .WithSlug("happy-paws")
            .WithDescription("A shelter for happy animals")
            .Build();

        // Act
        var response = await _platformAdminClient.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateOrganizationResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.OrganizationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateOrganization_DuplicateSlug_ReturnsConflict()
    {
        // Arrange
        var slug = $"dup-{Guid.NewGuid():N}"[..20];
        await CreateOrgAsync(slug: slug);

        var request = CreateOrganizationRequestBuilder.Default()
            .WithName("Another Org")
            .WithSlug(slug)
            .Build();

        // Act
        var response = await _platformAdminClient.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateOrganization_AsAdmin_Returns403()
    {
        // Arrange
        var request = CreateOrganizationRequestBuilder.Default()
            .WithSlug("admin-attempt")
            .Build();

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrganization_AsRegularUser_Returns403()
    {
        // Arrange
        var request = CreateOrganizationRequestBuilder.Default()
            .WithSlug("user-attempt")
            .Build();

        // Act
        var response = await _regularClient.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrganization_WhenNotAuthenticated_Returns401()
    {
        // Arrange
        var request = CreateOrganizationRequestBuilder.Default()
            .WithSlug("unauth-attempt")
            .Build();

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/organizations (List)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrganizations_AsPlatformAdmin_ReturnsList()
    {
        // Arrange
        await CreateOrgAsync(name: "List Test Org");

        // Act
        var response = await _platformAdminClient.GetAsync("/api/organizations?skip=0&take=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetOrganizationsResponseDto>();
        result.Should().NotBeNull();
        result!.Organizations.Should().NotBeEmpty();
        result.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetOrganizations_AsRegularUser_Returns403()
    {
        // Act
        var response = await _regularClient.GetAsync("/api/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/organizations/{id} (Get by ID)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrganizationById_AsPlatformAdmin_ReturnsOrg()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Detail Org");

        // Act
        var response = await _platformAdminClient.GetAsync($"/api/organizations/{created.OrganizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Detail Org");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizationById_NonExistent_Returns404()
    {
        // Act
        var response = await _platformAdminClient.GetAsync($"/api/organizations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // PUT /api/organizations/{id} (Update)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrganization_AsPlatformAdmin_ReturnsSuccess()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Before Update");

        // Act
        var response = await _platformAdminClient.PutAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}",
            new { Name = "After Update", Description = "Updated description" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CommandResponseDto>();
        result!.Success.Should().BeTrue();

        // Verify
        var getResponse = await _platformAdminClient.GetAsync($"/api/organizations/{created.OrganizationId}");
        var org = await getResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        org!.Name.Should().Be("After Update");
        org.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateOrganization_NonExistent_Returns404()
    {
        // Act
        var response = await _platformAdminClient.PutAsJsonAsync(
            $"/api/organizations/{Guid.NewGuid()}",
            new { Name = "No Org", Description = (string?)null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/organizations/{id}/deactivate & activate
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsPlatformAdmin_TogglesStatus()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Toggle Org");

        // Act - Deactivate
        var deactivateResponse = await _platformAdminClient.PostAsync(
            $"/api/organizations/{created.OrganizationId}/deactivate", null);

        // Assert
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await _platformAdminClient.GetAsync($"/api/organizations/{created.OrganizationId}");
        var org = await getResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        org!.IsActive.Should().BeFalse();

        // Act - Activate
        var activateResponse = await _platformAdminClient.PostAsync(
            $"/api/organizations/{created.OrganizationId}/activate", null);

        // Assert
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse = await _platformAdminClient.GetAsync($"/api/organizations/{created.OrganizationId}");
        org = await getResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        org!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_NonExistent_Returns404()
    {
        // Act
        var response = await _platformAdminClient.PostAsync(
            $"/api/organizations/{Guid.NewGuid()}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/organizations/{id}/members (Add member)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMember_AsPlatformAdmin_ReturnsSuccess()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Member Org");

        // Act
        var response = await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Admin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CommandResponseDto>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddMember_DuplicateMember_ReturnsConflict()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Dup Member Org");
        await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Moderator" });

        // Act
        var response = await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Admin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddMember_AsRegularUser_Returns403()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Auth Test Org");

        // Act
        var response = await _regularClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Admin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/organizations/{id}/members (List members)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMembers_AsPlatformAdmin_ReturnsMemberList()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Members List Org");
        await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Admin" });

        // Act
        var response = await _platformAdminClient.GetAsync(
            $"/api/organizations/{created.OrganizationId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetMembersResponseDto>();
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(1);
        result.Members[0].UserId.Should().Be(_regularUserId);
        result.Members[0].Role.Should().Be("Admin");
    }

    // ──────────────────────────────────────────────────────────────
    // DELETE /api/organizations/{id}/members/{userId} (Remove member)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMember_AsPlatformAdmin_ReturnsSuccess()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Remove Member Org");
        await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Moderator" });

        // Act
        var response = await _platformAdminClient.DeleteAsync(
            $"/api/organizations/{created.OrganizationId}/members/{_regularUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CommandResponseDto>();
        result!.Success.Should().BeTrue();

        // Verify member was removed
        var membersResponse = await _platformAdminClient.GetAsync(
            $"/api/organizations/{created.OrganizationId}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<GetMembersResponseDto>();
        members!.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveMember_NonExistent_Returns404()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "Remove Nonexist Org");

        // Act
        var response = await _platformAdminClient.DeleteAsync(
            $"/api/organizations/{created.OrganizationId}/members/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/organizations/mine (My organizations)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrganizations_AsAuthenticatedUser_ReturnsUserOrgs()
    {
        // Arrange
        var created = await CreateOrgAsync(name: "My Org Test");
        await _platformAdminClient.PostAsJsonAsync(
            $"/api/organizations/{created.OrganizationId}/members",
            new { UserId = _regularUserId, Role = "Moderator" });

        // Act
        var response = await _regularClient.GetAsync("/api/organizations/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetMyOrganizationsResponseDto>();
        result.Should().NotBeNull();
        result!.Organizations.Should().Contain(o => o.OrganizationName == "My Org Test");
    }

    [Fact]
    public async Task GetMyOrganizations_WhenNotAuthenticated_Returns401()
    {
        // Act
        var response = await _unauthenticatedClient.GetAsync("/api/organizations/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private record CreateOrganizationResponseDto(bool Success, Guid OrganizationId, string Message);
    private record CommandResponseDto(bool Success, string Message);

    private record GetOrganizationsResponseDto(
        List<OrganizationListItemDto> Organizations,
        long Total,
        int Skip,
        int Take);

    private record OrganizationListItemDto(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);

    private record OrganizationDetailDto(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    private record GetMembersResponseDto(List<MemberItemDto> Members);
    private record MemberItemDto(Guid Id, Guid OrganizationId, string UserId, string Role, DateTime JoinedAt);

    private record GetMyOrganizationsResponseDto(List<MyOrgItemDto> Organizations);
    private record MyOrgItemDto(Guid OrganizationId, string OrganizationName, string Slug, string Role, DateTime JoinedAt);
}
