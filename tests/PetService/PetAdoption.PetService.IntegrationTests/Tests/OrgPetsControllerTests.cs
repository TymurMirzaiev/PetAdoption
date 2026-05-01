using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class OrgPetsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public OrgPetsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "test-org-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_ShouldCreatePetInOrg()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("OrgBuddy")
            .WithPetTypeId(petTypeId)
            .WithTags("friendly", "vaccinated")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ──────────────────────────────────────────────────────────────
    // List
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ShouldReturnOnlyOrgPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("OrgPet1")
            .WithPetTypeId(petTypeId)
            .Build();
        await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Act
        var response = await _client.GetAsync($"/api/organizations/{TestOrgId}/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrgPetsResponseDto>();
        result!.Pets.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Authorization
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithWrongOrg_ShouldReturnForbidden()
    {
        // Arrange
        var wrongOrgId = Guid.NewGuid();
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithPetTypeId(petTypeId)
            .Build();

        // Act - user's org claim doesn't match route org
        var response = await _client.PostAsJsonAsync($"/api/organizations/{wrongOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutOrgClaims_ShouldReturnForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(role: "User"));

        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithPetTypeId(petTypeId)
            .Build();

        // Act
        var response = await client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken());

        var response = await adminClient.PostAsJsonAsync("/api/admin/pet-types", new { Code = "dog", Name = "Dog" });
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var allTypesResponse = await adminClient.GetAsync("/api/admin/pet-types?includeInactive=true");
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        return allTypes!.First(t => t.Code == "dog").Id;
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record OrgPetsResponseDto(List<OrgPetItemDto> Pets, long Total, int Skip, int Take);
    private record OrgPetItemDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
    private record CreatePetTypeResponseDto(Guid Id);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
}
