using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class OrganizationMetricsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;

    private static readonly Guid TestOrganizationId = Guid.NewGuid();

    public OrganizationMetricsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        // Bootstrap the host so EnsureCreatedAsync + PetTypeSeeder run.
        using var bootstrapClient = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
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

    private async Task<Guid> SeedPetWithOrgAsync(string name, Guid organizationId)
    {
        await using var db = _factory.CreateDbContext();
        var petTypeId = db.PetTypes.First().Id;
        var pet = Pet.Create(name, petTypeId, breed: null, ageMonths: 24, description: null);
        pet.AssignToOrganization(organizationId);
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        return pet.Id;
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
        var petId = await SeedPetWithOrgAsync("MetricsPet", TestOrganizationId);
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
        var petId = await SeedPetWithOrgAsync("MetricsPet2", TestOrganizationId);
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
