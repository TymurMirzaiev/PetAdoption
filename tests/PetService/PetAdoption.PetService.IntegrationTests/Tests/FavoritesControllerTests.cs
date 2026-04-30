using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class FavoritesControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public FavoritesControllerTests(SqlServerFixture sqlFixture)
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

        // Type already seeded — look it up
        var allTypesResponse = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");
        allTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        var existing = allTypes!.First(t => t.Code == code);
        return existing.Id;
    }

    private async Task<Guid> CreatePetAsync(string name = "Buddy", Guid? petTypeId = null)
    {
        var typeId = petTypeId ?? await SeedPetTypeAsync();

        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(typeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        result.Should().NotBeNull();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/favorites (Add Favorite)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddFavorite_WithValidPet_ReturnsCreated()
    {
        // Arrange
        var petId = await CreatePetAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AddFavoriteResponseDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.PetId.Should().Be(petId);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsConflict()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddFavorite_WithNonExistentPet_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // DELETE /api/favorites/{petId} (Remove Favorite)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFavorite_Existing_ReturnsNoContent()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.DeleteAsync($"/api/favorites/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveFavorite_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/favorites/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/favorites (Get Favorites)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFavorites_WithFavorites_ReturnsList()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.GetAsync("/api/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterThan(0);
        body.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFavorites_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFavorites_AfterRemove_ExcludesRemovedFavorite()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId1 = await CreatePetAsync("Pet1", petTypeId);
        var petId2 = await CreatePetAsync("Pet2", petTypeId);
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId1 });
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId2 });
        await _client.DeleteAsync($"/api/favorites/{petId1}");

        // Act
        var response = await _client.GetAsync("/api/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(1);
        body.Items.First().PetId.Should().Be(petId2);
    }

    // ──────────────────────────────────────────────────────────────
    // Auth
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddFavorite_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/favorites", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFavorites_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFavorite_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/favorites/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record AddFavoriteResponseDto(Guid Id, Guid PetId, DateTime CreatedAt);
    private record FavoriteItemDto(Guid FavoriteId, Guid PetId, string PetName, string PetType, string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
    private record GetFavoritesResponseDto(List<FavoriteItemDto> Items, long TotalCount, int Page, int PageSize);
}
