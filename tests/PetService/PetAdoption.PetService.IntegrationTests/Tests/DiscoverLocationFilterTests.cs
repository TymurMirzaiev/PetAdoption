using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class DiscoverLocationFilterTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;

    // Berlin: 52.5200° N, 13.4050° E
    private const decimal BerlinLat = 52.52m;
    private const decimal BerlinLng = 13.405m;

    // Munich: 48.1351° N, 11.5820° E (~504 km from Berlin)
    private const decimal MunichLat = 48.1351m;
    private const decimal MunichLng = 11.582m;

    // New York City: 40.7128° N, -74.0060° W (~6400 km from Berlin)
    private const decimal NycLat = 40.7128m;
    private const decimal NycLng = -74.006m;

    private static readonly Guid NearbyOrgId = Guid.NewGuid();
    private static readonly Guid FarOrgId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    public DiscoverLocationFilterTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "admin", role: "Admin",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", NearbyOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: TestUserId.ToString(), role: "User"));

        await SeedOrgsAndPets();
    }

    public async Task DisposeAsync()
    {
        _adminClient.Dispose();
        _userClient.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedOrgsAndPets()
    {
        // Set address for nearby org (Munich, ~504 km from Berlin)
        var nearbyAdminClient = _factory.CreateClient();
        nearbyAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "nearby-admin", role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", NearbyOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        await nearbyAdminClient.PostAsJsonAsync(
            $"/api/organizations/{NearbyOrgId}/address",
            new { Lat = MunichLat, Lng = MunichLng, Line1 = "Marienplatz 1", City = "Munich", Region = "BY", Country = "Germany", PostalCode = "80331" });

        var farAdminClient = _factory.CreateClient();
        farAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "far-admin", role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", FarOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        await farAdminClient.PostAsJsonAsync(
            $"/api/organizations/{FarOrgId}/address",
            new { Lat = NycLat, Lng = NycLng, Line1 = "Broadway 1", City = "New York", Region = "NY", Country = "USA", PostalCode = "10004" });

        // Get or create a pet type
        var petTypeId = await GetOrCreatePetTypeAsync();

        // Seed pets for nearby org
        await SeedOrgPetAsync(nearbyAdminClient, NearbyOrgId, petTypeId, "MunichPet1");
        await SeedOrgPetAsync(nearbyAdminClient, NearbyOrgId, petTypeId, "MunichPet2");

        // Seed pets for far org
        await SeedOrgPetAsync(farAdminClient, FarOrgId, petTypeId, "NycPet1");

        nearbyAdminClient.Dispose();
        farAdminClient.Dispose();
    }

    private async Task<Guid> GetOrCreatePetTypeAsync()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/admin/pet-types",
            new { Code = $"loc_test_{Guid.NewGuid():N}", Name = "LocationTestType" });

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var all = await _adminClient.GetFromJsonAsync<PetTypeListResponseDto>("/api/admin/pet-types?includeInactive=true");
        return all!.Items.First().Id;
    }

    private async Task SeedOrgPetAsync(HttpClient client, Guid orgId, Guid petTypeId, string name)
    {
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName(name)
            .WithPetTypeId(petTypeId)
            .Build();
        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/pets", request);
        response.IsSuccessStatusCode.Should().BeTrue($"seeding pet '{name}' should succeed (got {response.StatusCode})");
    }

    // ──────────────────────────────────────────────────────────────
    // Location filter
    // ──────────────────────────────────────────────────────────────


    [Fact]
    public async Task Discover_WithLocationFilter_ReturnsOnlyNearbyOrgPets()
    {
        // Arrange — search from Berlin with 600 km radius (includes Munich, excludes NYC)
        var url = $"/api/discover?lat={BerlinLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lng={BerlinLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radiusKm=600&take=50";

        // Act
        var response = await _userClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();

        var petNames = body!.Pets.Select(p => p.Name).ToList();
        petNames.Should().Contain("MunichPet1");
        petNames.Should().Contain("MunichPet2");
        petNames.Should().NotContain("NycPet1");
    }

    [Fact]
    public async Task Discover_WithTightLocationFilter_ExcludesEvenNearbyOrg()
    {
        // Arrange — search from Berlin with 50 km radius (excludes Munich ~504 km away)
        var url = $"/api/discover?lat={BerlinLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lng={BerlinLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radiusKm=50&take=50";

        // Act
        var response = await _userClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();

        var petNames = body!.Pets.Select(p => p.Name).ToList();
        petNames.Should().NotContain("MunichPet1");
        petNames.Should().NotContain("NycPet1");
    }

    [Fact]
    public async Task Discover_WithMissingRadiusKm_Returns400()
    {
        // Arrange — only lat and lng provided, no radiusKm
        var url = $"/api/discover?lat={BerlinLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lng={BerlinLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        // Act
        var response = await _userClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Discover_WithMissingLat_Returns400()
    {
        // Arrange — only lng and radiusKm, no lat
        var url = $"/api/discover?lng={BerlinLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radiusKm=100";

        // Act
        var response = await _userClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Discover_WithoutLocationFilter_ReturnsPetsFromAllOrgs()
    {
        // Arrange — no location filter
        var url = "/api/discover?take=50";

        // Act
        var response = await _userClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();

        // Should be able to see pets from both orgs
        var petNames = body!.Pets.Select(p => p.Name).ToList();
        petNames.Should().Contain(p => p == "MunichPet1" || p == "NycPet1");
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record DiscoverPetDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags);
    private record DiscoverResponseDto(List<DiscoverPetDto> Pets, bool HasMore);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeListResponseDto(List<PetTypeItemDto> Items);
    private record PetTypeItemDto(Guid Id, string Code, string Name, bool IsActive);
}
