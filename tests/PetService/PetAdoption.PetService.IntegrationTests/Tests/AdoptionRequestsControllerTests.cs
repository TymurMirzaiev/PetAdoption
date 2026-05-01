using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class AdoptionRequestsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _userClient = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly Guid TestOrganizationId = Guid.NewGuid();

    public AdoptionRequestsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: TestUserId,
                role: "Admin",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrganizationId.ToString() },
                    { "orgRole", "Admin" }
                }));
        await Task.CompletedTask;
    }

    private HttpClient CreateClientWithOrg(Guid? orgId = null, string? orgRole = "Admin", string? userId = null)
    {
        var client = _factory.CreateClient();
        var claims = new Dictionary<string, string>();
        if (orgId is not null) claims["organizationId"] = orgId.Value.ToString();
        if (orgRole is not null) claims["orgRole"] = orgRole;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: userId ?? Guid.NewGuid().ToString(),
                role: "Admin",
                additionalClaims: claims.Count > 0 ? claims : null));
        return client;
    }

    public async Task DisposeAsync()
    {
        _userClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync(string code = "ar_dog", string name = "Adoption Request Dog")
    {
        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName(name)
            .Build();

        var response = await _userClient.PostAsJsonAsync("/api/admin/pet-types", request);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var allTypesResponse = await _userClient.GetAsync("/api/admin/pet-types?includeInactive=true");
        allTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        return allTypes!.First(t => t.Code == code).Id;
    }

    /// <summary>
    /// Seeds a pet directly in the database with an OrganizationId (the API does not expose this field).
    /// </summary>
    private async Task<Guid> SeedPetWithOrgAsync(string name, Guid petTypeId, Guid organizationId)
    {
        await using var db = _factory.CreateDbContext();
        var pet = Pet.Create(name, petTypeId, breed: null, ageMonths: 24, description: null, organizationId: organizationId);
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        return pet.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests (Create)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAdoptionRequest_WithValidData_ReturnsCreated()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("Buddy", petTypeId, TestOrganizationId);

        // Act
        var response = await _userClient.PostAsJsonAsync(
            "/api/adoption-requests",
            new { PetId = petId, Message = "I'd love to adopt Buddy!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateAdoptionRequest_DuplicatePending_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("Buddy2", petTypeId, TestOrganizationId);
        await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "First request" });

        // Act
        var response = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Second request" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAdoptionRequest_NonExistentPet_ReturnsNotFound()
    {
        // Act
        var response = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = Guid.NewGuid(), Message = "Hi" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAdoptionRequest_PetWithoutOrganization_ReturnsConflict()
    {
        // Arrange — pet created via API has no OrganizationId
        var petTypeId = await SeedPetTypeAsync();
        var createPetRequest = new CreatePetRequestBuilder()
            .WithName("OrgLess")
            .WithPetTypeId(petTypeId)
            .Build();
        var createPetResponse = await _userClient.PostAsJsonAsync("/api/pets", createPetRequest);
        createPetResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await createPetResponse.Content.ReadFromJsonAsync<CreatePetResponseDto>();

        // Act
        var response = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = pet!.Id, Message = "Hi" });

        // Assert — handler raises invalid_operation -> 409
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/adoption-requests/mine
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAdoptionRequests_ReturnsCallerRequests()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("MineTestPet", petTypeId, TestOrganizationId);
        await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Please!" });

        // Act
        var response = await _userClient.GetFromJsonAsync<AdoptionRequestListDto>("/api/adoption-requests/mine");

        // Assert
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.Items.Should().Contain(i => i.PetId == petId);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/adoption-requests/organization/{id}
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrgAdoptionRequests_FilteredByStatus_ReturnsMatchingItems()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("OrgPet", petTypeId, TestOrganizationId);
        await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Hi" });

        // Act
        var response = await _userClient.GetFromJsonAsync<OrgAdoptionRequestListDto>(
            $"/api/adoption-requests/organization/{TestOrganizationId}?status=Pending");

        // Assert
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.Items.Should().OnlyContain(i => i.Status == "Pending");
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests/{id}/approve
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAdoptionRequest_WhenPending_ReturnsApprovedAndReservesPet()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("ApprovePet", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Approve me" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        // Act
        var response = await _userClient.PostAsync($"/api/adoption-requests/{created!.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();
        body!.Status.Should().Be("Approved");

        // And the pet should now be Reserved
        var petResponse = await _userClient.GetAsync($"/api/pets/{petId}");
        petResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var petBody = await petResponse.Content.ReadFromJsonAsync<PetStatusDto>();
        petBody!.Status.Should().Be("Reserved");
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests/{id}/reject
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAdoptionRequest_WhenPending_ReturnsRejected()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("RejectPet", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Reject me" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        // Act
        var response = await _userClient.PostAsJsonAsync(
            $"/api/adoption-requests/{created!.Id}/reject",
            new { Reason = "Not a fit." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();
        body!.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task RejectAdoptionRequest_WithEmptyReason_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("RejectPet2", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Reject me" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        // Act
        var response = await _userClient.PostAsJsonAsync(
            $"/api/adoption-requests/{created!.Id}/reject",
            new { Reason = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests/{id}/cancel
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAdoptionRequest_ByOwner_ReturnsCancelled()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("CancelPet", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Changed my mind" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        // Act
        var response = await _userClient.PostAsync($"/api/adoption-requests/{created!.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();
        body!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelAdoptionRequest_ByDifferentUser_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("CancelOtherPet", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Mine" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        using var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: Guid.NewGuid().ToString(), role: "Admin"));

        // Act
        var response = await otherClient.PostAsync($"/api/adoption-requests/{created!.Id}/cancel", null);

        // Assert — invalid_operation -> 409
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────
    // Auth
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAdoptionRequest_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        using var anonClient = _factory.CreateClient();

        // Act
        var response = await anonClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = Guid.NewGuid(), Message = "Hi" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApproveAdoptionRequest_WithDifferentOrg_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("DiffOrgApprove", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Will be denied at approval" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        using var otherOrgClient = CreateClientWithOrg(orgId: Guid.NewGuid(), orgRole: "Admin");

        // Act
        var response = await otherOrgClient.PostAsync($"/api/adoption-requests/{created!.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveAdoptionRequest_WithoutOrgClaims_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("NoOrgClaims", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "No claims approve" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        using var noClaimsClient = CreateClientWithOrg(orgId: null, orgRole: null);

        // Act
        var response = await noClaimsClient.PostAsync($"/api/adoption-requests/{created!.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectAdoptionRequest_WithDifferentOrg_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await SeedPetWithOrgAsync("DiffOrgReject", petTypeId, TestOrganizationId);
        var createResponse = await _userClient.PostAsJsonAsync("/api/adoption-requests",
            new { PetId = petId, Message = "Will be denied at reject" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();

        using var otherOrgClient = CreateClientWithOrg(orgId: Guid.NewGuid(), orgRole: "Admin");

        // Act
        var response = await otherOrgClient.PostAsJsonAsync(
            $"/api/adoption-requests/{created!.Id}/reject",
            new { Reason = "no" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrgAdoptionRequests_WithDifferentOrg_ReturnsForbidden()
    {
        // Arrange
        using var otherOrgClient = CreateClientWithOrg(orgId: Guid.NewGuid(), orgRole: "Admin");

        // Act
        var response = await otherOrgClient.GetAsync(
            $"/api/adoption-requests/organization/{TestOrganizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrgAdoptionRequests_WithoutOrgClaims_ReturnsForbidden()
    {
        // Arrange
        using var noClaimsClient = CreateClientWithOrg(orgId: null, orgRole: null);

        // Act
        var response = await noClaimsClient.GetAsync(
            $"/api/adoption-requests/organization/{TestOrganizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record AdoptionRequestResultDto(Guid Id, string Status);

    private record AdoptionRequestListDto(
        List<AdoptionRequestItemDto> Items, long Total, int Skip, int Take);

    private record AdoptionRequestItemDto(
        Guid Id, Guid PetId, string PetName, string PetType, Guid OrganizationId,
        string Status, string? Message, string? RejectionReason,
        DateTime CreatedAt, DateTime? ReviewedAt);

    private record OrgAdoptionRequestListDto(
        List<OrgAdoptionRequestItemDto> Items, long Total, int Skip, int Take);

    private record OrgAdoptionRequestItemDto(
        Guid Id, Guid UserId, Guid PetId, string PetName,
        string Status, string? Message, DateTime CreatedAt);

    private record PetStatusDto(string Status);

    private record CreatePetResponseDto(Guid Id);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
}
