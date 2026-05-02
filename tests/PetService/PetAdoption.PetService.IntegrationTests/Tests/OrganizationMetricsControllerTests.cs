using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

internal class OrganizationMetricsControllerTests : IntegrationTestBase
{
    private static readonly Guid TestOrganizationId = Guid.NewGuid();

    public OrganizationMetricsControllerTests(SqlServerFixture sqlFixture) : base(sqlFixture) { }

    public override Task InitializeAsync()
    {
        // base.InitializeAsync() creates _factory and _client; _client acts as bootstrap
        base.InitializeAsync();
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private HttpClient CreateClientWithOrg(Guid? orgId, string? orgRole = "Admin")
    {
        var client = _factory.CreateClient();
        var claims = new Dictionary<string, string>();
        if (orgId is not null) claims["organizationId"] = orgId.Value.ToString();
        if (orgRole is not null) claims["orgRole"] = orgRole;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: Guid.NewGuid().ToString(),
                role: "Admin",
                additionalClaims: claims.Count > 0 ? claims : null));
        return client;
    }


    // ──────────────────────────────────────────────────────────────
    // GET /api/organizations/{orgId}/metrics
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrgMetrics_AsOrgMember_ReturnsOk()
    {
        // Arrange
        using var client = CreateClientWithOrg(TestOrganizationId);

        // Act
        var response = await client.GetAsync($"/api/organizations/{TestOrganizationId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrgMetrics_AsDifferentOrg_ReturnsForbidden()
    {
        // Arrange
        using var client = CreateClientWithOrg(Guid.NewGuid());

        // Act
        var response = await client.GetAsync($"/api/organizations/{TestOrganizationId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrgMetrics_WithoutOrgClaims_ReturnsForbidden()
    {
        // Arrange
        using var client = CreateClientWithOrg(orgId: null, orgRole: null);

        // Act
        var response = await client.GetAsync($"/api/organizations/{TestOrganizationId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/pets/{petId}/metrics
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPetMetrics_AsOwningOrgMember_ReturnsOk()
    {
        // Arrange
        var petId = (await SeedPetWithOrgAsync(TestOrganizationId, "MetricsPet")).Id;
        using var client = CreateClientWithOrg(TestOrganizationId);

        // Act
        var response = await client.GetAsync($"/api/pets/{petId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPetMetrics_AsDifferentOrg_ReturnsForbidden()
    {
        // Arrange
        var petId = (await SeedPetWithOrgAsync(TestOrganizationId, "MetricsPet2")).Id;
        using var client = CreateClientWithOrg(Guid.NewGuid());

        // Act
        var response = await client.GetAsync($"/api/pets/{petId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPetMetrics_PetWithoutOrg_ReturnsForbidden()
    {
        // Arrange — pet without organization
        await using var db = _factory.CreateDbContext();
        var petTypeId = db.PetTypes.First().Id;
        var orphanPet = Pet.Create("OrgLess", petTypeId, breed: null, ageMonths: 12, description: null);
        db.Pets.Add(orphanPet);
        await db.SaveChangesAsync();

        using var client = CreateClientWithOrg(TestOrganizationId);

        // Act
        var response = await client.GetAsync($"/api/pets/{orphanPet.Id}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
