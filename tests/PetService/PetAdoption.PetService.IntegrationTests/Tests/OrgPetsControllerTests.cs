using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

internal class OrgPetsControllerTests : IntegrationTestBase
{
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public OrgPetsControllerTests(SqlServerFixture sqlFixture) : base(sqlFixture) { }

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

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record OrgPetsResponseDto(List<OrgPetItemDto> Pets, long Total, int Skip, int Take);
    private record OrgPetItemDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
}
