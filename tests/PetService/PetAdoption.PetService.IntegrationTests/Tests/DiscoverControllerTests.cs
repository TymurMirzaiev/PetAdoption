using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class DiscoverControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public DiscoverControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: TestUserId));
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync(string code = "dog", string name = "Dog")
    {
        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName(name)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var allTypesResponse = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");
        allTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        var existing = allTypes!.First(t => t.Code == code);
        return existing.Id;
    }

    private async Task<Guid> CreatePetAsync(string name = "Buddy", Guid? petTypeId = null, int? ageMonths = null)
    {
        var typeId = petTypeId ?? await SeedPetTypeAsync();

        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(typeId);

        if (ageMonths.HasValue)
            request = request.WithAgeMonths(ageMonths.Value);

        var response = await _client.PostAsJsonAsync("/api/pets", request.Build());
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        result.Should().NotBeNull();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/discover (Discovery Feed)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_WithAvailablePets_ReturnsPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync("DiscoverTest1", petTypeId);
        await CreatePetAsync("DiscoverTest2", petTypeId);

        // Act
        var response = await _client.GetAsync("/api/discover?take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        body!.Pets.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Discover_ExcludesFavoritedPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId1 = await CreatePetAsync("FavExclude1", petTypeId);
        var petId2 = await CreatePetAsync("FavExclude2", petTypeId);

        // Favorite pet1
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId1 });

        // Act
        var response = await _client.GetAsync("/api/discover?take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().NotContain(petId1);
        petIds.Should().Contain(petId2);
    }

    [Fact]
    public async Task Discover_ExcludesSkippedPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId1 = await CreatePetAsync("SkipExclude1", petTypeId);
        var petId2 = await CreatePetAsync("SkipExclude2", petTypeId);

        // Skip pet1
        await _client.PostAsJsonAsync("/api/skips", new { PetId = petId1 });

        // Act
        var response = await _client.GetAsync("/api/discover?take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().NotContain(petId1);
        petIds.Should().Contain(petId2);
    }

    [Fact]
    public async Task Discover_WhenAllPetsSeen_ReturnsEmpty()
    {
        // Arrange -- use a unique user to avoid interference from other tests
        var uniqueUserId = Guid.NewGuid().ToString();
        var uniqueClient = _factory.CreateClient();
        uniqueClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: uniqueUserId));

        var petTypeId = await SeedPetTypeAsync();

        // Get all discoverable pets and skip/favorite them all
        var initialResponse = await uniqueClient.GetAsync("/api/discover?take=1000");
        var initialBody = await initialResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();

        foreach (var pet in initialBody!.Pets)
        {
            await uniqueClient.PostAsJsonAsync("/api/skips", new { PetId = pet.Id });
        }

        // Act
        var response = await uniqueClient.GetAsync("/api/discover?take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        body!.Pets.Should().BeEmpty();
        body.HasMore.Should().BeFalse();

        uniqueClient.Dispose();
    }

    [Fact]
    public async Task Discover_WithPetTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("dog", "Dog");
        var catTypeId = await SeedPetTypeAsync("cat", "Cat");
        var dogPetId = await CreatePetAsync("FilterDog", dogTypeId);
        var catPetId = await CreatePetAsync("FilterCat", catTypeId);

        // Act -- filter by cat type only
        var response = await _client.GetAsync($"/api/discover?petTypeId={catTypeId}&take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().Contain(catPetId);
        petIds.Should().NotContain(dogPetId);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/skips (Track Skip)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrackSkip_WithValidPet_ReturnsCreated()
    {
        // Arrange
        var petId = await CreatePetAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TrackSkipResponseDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.PetId.Should().Be(petId);
    }

    [Fact]
    public async Task TrackSkip_Duplicate_ReturnsConflict()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TrackSkip_NonExistentPet_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // DELETE /api/skips (Reset Skips)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetSkips_AfterSkipping_PetsReappearInDiscovery()
    {
        // Arrange
        var uniqueUserId = Guid.NewGuid().ToString();
        var uniqueClient = _factory.CreateClient();
        uniqueClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: uniqueUserId));

        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("ResetTest", petTypeId);

        // Skip the pet
        await uniqueClient.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Verify it's excluded
        var beforeResponse = await uniqueClient.GetAsync("/api/discover?take=100");
        var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        beforeBody!.Pets.Select(p => p.Id).Should().NotContain(petId);

        // Act -- reset skips
        var resetResponse = await uniqueClient.DeleteAsync("/api/skips");

        // Assert
        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResponse = await uniqueClient.GetAsync("/api/discover?take=100");
        var afterBody = await afterResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        afterBody!.Pets.Select(p => p.Id).Should().Contain(petId);

        uniqueClient.Dispose();
    }

    // ──────────────────────────────────────────────────────────────
    // Filter: breed
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_WithBreedFilter_ReturnsOnlyMatchingBreed()
    {
        // Arrange
        var typeId = await SeedPetTypeAsync(code: "breed_dog", name: "Breed Dog");
        var goldenRequest = new CreatePetRequestBuilder()
            .WithName($"Goldie-{Guid.NewGuid():N}").WithPetTypeId(typeId).WithBreed("Golden Retriever").Build();
        var huskyRequest = new CreatePetRequestBuilder()
            .WithName($"Husk-{Guid.NewGuid():N}").WithPetTypeId(typeId).WithBreed("Siberian Husky").Build();
        await _client.PostAsJsonAsync("/api/pets", goldenRequest);
        await _client.PostAsJsonAsync("/api/pets", huskyRequest);

        // Act
        var response = await _client.GetAsync("/api/discover?breed=Golden&take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        body!.Pets.Should().OnlyContain(p => p.Breed != null && p.Breed.Contains("Golden"));
    }

    // ──────────────────────────────────────────────────────────────
    // Ordering
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_TwoCalls_ReturnPetsInDeterministicOrder()
    {
        // Arrange
        var typeId = await SeedPetTypeAsync(code: "order_dog", name: "Order Dog");
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/pets",
                new CreatePetRequestBuilder().WithName($"Order-{Guid.NewGuid():N}").WithPetTypeId(typeId).Build());
        }

        // Act
        var first = await _client.GetFromJsonAsync<DiscoverResponseDto>("/api/discover?take=100");
        var second = await _client.GetFromJsonAsync<DiscoverResponseDto>("/api/discover?take=100");

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.Pets.Select(p => p.Id).Should().Equal(second!.Pets.Select(p => p.Id));
    }

    // ──────────────────────────────────────────────────────────────
    // Auth
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/discover");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrackSkip_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/skips", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetSkips_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/skips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record DiscoverPetDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags);
    private record DiscoverResponseDto(List<DiscoverPetDto> Pets, bool HasMore);
    private record TrackSkipResponseDto(Guid Id, Guid PetId, DateTime CreatedAt);
}
