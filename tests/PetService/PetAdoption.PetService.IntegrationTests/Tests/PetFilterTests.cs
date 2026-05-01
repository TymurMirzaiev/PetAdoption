using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PetFilterTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _dogTypeId;
    private Guid _catTypeId;

    public PetFilterTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(role: "Admin"));
        await SeedTestData();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync(string code, string name)
    {
        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName(name)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<PetTypeItem>();
            return result!.Id;
        }

        var allTypesResponse = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeItem>>();
        return allTypes!.First(t => t.Code == code).Id;
    }

    private async Task SeedTestData()
    {
        _dogTypeId = await SeedPetTypeAsync("dog", "Dog");
        _catTypeId = await SeedPetTypeAsync("cat", "Cat");

        await CreatePet("Buddy", _dogTypeId, "Golden Retriever", 24);
        await CreatePet("Max", _dogTypeId, "German Shepherd", 36);
        await CreatePet("Whiskers", _catTypeId, "Siamese", 12);
        await CreatePet("Luna", _catTypeId, "Persian", 6);
        await CreatePet("Rocky", _dogTypeId, "Bulldog", 48);
    }

    private async Task CreatePet(string name, Guid typeId, string breed, int ageMonths)
    {
        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(typeId)
            .WithBreed(breed)
            .WithAgeMonths(ageMonths)
            .Build();

        await _client.PostAsJsonAsync("/api/pets", request);
    }

    // ──────────────────────────────────────────────────────────────
    // Baseline
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_NoFilters_ReturnsAllPets()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(5);
    }

    // ──────────────────────────────────────────────────────────────
    // Age Filters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithMinAge_FiltersCorrectly()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?minAge=24");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(3);
        response.Pets.Should().OnlyContain(p => p.AgeMonths >= 24);
    }

    [Fact]
    public async Task GetPets_WithMaxAge_FiltersCorrectly()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?maxAge=12");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(2);
        response.Pets.Should().OnlyContain(p => p.AgeMonths <= 12);
    }

    [Fact]
    public async Task GetPets_WithAgeRange_FiltersCorrectly()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?minAge=12&maxAge=36");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(3);
        response.Pets.Should().OnlyContain(p => p.AgeMonths >= 12 && p.AgeMonths <= 36);
    }

    // ──────────────────────────────────────────────────────────────
    // Breed Search
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithBreedSearch_FiltersCorrectly()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?breed=Golden");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(1);
        response.Pets.Should().OnlyContain(p => p.Breed != null && p.Breed.Contains("Golden"));
    }

    [Fact]
    public async Task GetPets_WithBreedSearchNoMatch_ReturnsEmpty()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?breed=Nonexistent");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPets_BreedSearchIsCaseInsensitive_FindsMatch()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?breed=golden");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(1);
        response.Pets[0].Breed.Should().Be("Golden Retriever");
    }

    // ──────────────────────────────────────────────────────────────
    // Combined Filters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithPetTypeAndAge_CombinesFilters()
    {
        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>(
            $"api/pets?petTypeId={_dogTypeId}&minAge=30");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().HaveCount(2);
        response.Pets.Should().AllSatisfy(p =>
        {
            p.Type.Should().Be("Dog");
            p.AgeMonths.Should().BeGreaterThanOrEqualTo(30);
        });
    }

    // ──────────────────────────────────────────────────────────────
    // Ordering
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_OrderedByNameAscending()
    {
        // Arrange
        var charlieRequest = new CreatePetRequestBuilder()
            .WithName("Charlie")
            .WithPetTypeId(_dogTypeId)
            .WithBreed("Poodle")
            .WithAgeMonths(18)
            .Build();
        var alphaRequest = new CreatePetRequestBuilder()
            .WithName("Alpha")
            .WithPetTypeId(_dogTypeId)
            .WithBreed("Poodle")
            .WithAgeMonths(18)
            .Build();
        var bravoRequest = new CreatePetRequestBuilder()
            .WithName("Bravo")
            .WithPetTypeId(_dogTypeId)
            .WithBreed("Poodle")
            .WithAgeMonths(18)
            .Build();

        await _client.PostAsJsonAsync("/api/pets", charlieRequest);
        await _client.PostAsJsonAsync("/api/pets", alphaRequest);
        await _client.PostAsJsonAsync("/api/pets", bravoRequest);

        // Act
        var response = await _client.GetFromJsonAsync<PetsResult>("api/pets?take=100");

        // Assert
        response.Should().NotBeNull();
        var names = response!.Pets.Select(p => p.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record PetsResult(List<PetItem> Pets, long Total, int Skip, int Take);
    private record PetItem(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);
    private record PetTypeItem(Guid Id, string Code, string Name, bool IsActive);
}
