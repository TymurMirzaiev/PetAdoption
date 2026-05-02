using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

internal class OrgDashboardControllerTests : IntegrationTestBase
{
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public OrgDashboardControllerTests(SqlServerFixture sqlFixture) : base(sqlFixture) { }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "test-org-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────
    // Dashboard KPI endpoint
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_WithSeededPets_ShouldReturnCorrectCounts()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await SeedOrgPetsAsync(petTypeId, availableCount: 2, adoptedCount: 1);

        // Act
        var response = await _client.GetAsync($"/api/organizations/{TestOrgId}/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardResponseDto>();
        result.Should().NotBeNull();
        result!.TotalPets.Should().BeGreaterThanOrEqualTo(3);
        result.AvailablePets.Should().BeGreaterThanOrEqualTo(2);
        result.AdoptedPets.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDashboard_ForNewOrg_ShouldReturnZeroCounts()
    {
        // Arrange
        var emptyOrgId = Guid.NewGuid();
        var emptyOrgClient = _factory.CreateClient();
        emptyOrgClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "empty-org-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", emptyOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        // Act
        var response = await emptyOrgClient.GetAsync($"/api/organizations/{emptyOrgId}/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardResponseDto>();
        result.Should().NotBeNull();
        result!.TotalPets.Should().Be(0);
        result.AvailablePets.Should().Be(0);
        result.AdoptedPets.Should().Be(0);
        result.AdoptionRate.Should().Be(0.0);
        result.AvgSwipeRate.Should().Be(0.0);
    }

    // ──────────────────────────────────────────────────────────────
    // Authorization
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_WithNonOrgUser_ShouldReturnForbidden()
    {
        // Arrange
        var nonOrgClient = _factory.CreateClient();
        nonOrgClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                PetServiceWebAppFactory.GenerateTestToken(userId: "non-org-user", role: "User"));

        // Act
        var response = await nonOrgClient.GetAsync($"/api/organizations/{TestOrgId}/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_WithWrongOrg_ShouldReturnForbidden()
    {
        // Arrange
        var wrongOrgId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/organizations/{wrongOrgId}/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // Trends endpoint
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboardTrends_WithDefaultRange_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync($"/api/organizations/{TestOrgId}/dashboard/trends");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrendsResponseDto>();
        result.Should().NotBeNull();
        result!.AdoptionsByWeek.Should().NotBeNull();
        result.RequestsByWeek.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardTrends_WithValidDateRange_ShouldReturnGroupedData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-28).ToString("O");
        var to = DateTime.UtcNow.ToString("O");

        // Act
        var response = await _client.GetAsync(
            $"/api/organizations/{TestOrgId}/dashboard/trends?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrendsResponseDto>();
        result.Should().NotBeNull();
        result!.AdoptionsByWeek.Should().NotBeNull();
        result.RequestsByWeek.Should().NotBeNull();
        // 4 weeks + week 0 = 5 points (0..4)
        result.AdoptionsByWeek.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task GetDashboardTrends_WithFromAfterTo_ShouldReturnBadRequest()
    {
        // Arrange
        var from = DateTime.UtcNow.ToString("O");
        var to = DateTime.UtcNow.AddDays(-1).ToString("O");

        // Act
        var response = await _client.GetAsync(
            $"/api/organizations/{TestOrgId}/dashboard/trends?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        // Assert
        // invalid_operation → 422 UnprocessableEntity
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task SeedOrgPetsAsync(Guid petTypeId, int availableCount, int adoptedCount)
    {
        await using var db = _factory.CreateDbContext();

        for (var i = 0; i < availableCount; i++)
        {
            var pet = Pet.Create($"AvailablePet_{Guid.NewGuid():N}", petTypeId);
            pet.AssignToOrganization(TestOrgId);
            db.Pets.Add(pet);
        }

        for (var i = 0; i < adoptedCount; i++)
        {
            var pet = Pet.Create($"AdoptedPet_{Guid.NewGuid():N}", petTypeId);
            pet.AssignToOrganization(TestOrgId);
            pet.Reserve();
            pet.Adopt();
            db.Pets.Add(pet);
        }

        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record DashboardResponseDto(
        int TotalPets,
        int AvailablePets,
        int ReservedPets,
        int AdoptedPets,
        int TotalAdoptionRequests,
        int PendingRequests,
        double AdoptionRate,
        long TotalImpressions,
        double AvgSwipeRate);

    private record TrendsResponseDto(
        List<TrendPointDto> AdoptionsByWeek,
        List<TrendPointDto> RequestsByWeek);

    private record TrendPointDto(DateTime WeekStart, string Label, int Count);

}
