using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class FavoritesEnhancedTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public FavoritesEnhancedTests(SqlServerFixture sqlFixture)
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

    private async Task<Guid> SeedPetTypeAsync(string code, string name)
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
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        return allTypes!.First(t => t.Code == code).Id;
    }

    private async Task<Guid> CreatePetAsync(string name, Guid petTypeId)
    {
        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        return result!.Id;
    }

    private async Task AddFavoriteAsync(Guid petId)
    {
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ──────────────────────────────────────────────────────────────
    // Sorting
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("newest")]
    [InlineData("oldest")]
    [InlineData("name")]
    public async Task GetFavorites_WithSortBy_ReturnsOk(string sortBy)
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("sorttest" + sortBy.GetHashCode(), "SortTest " + sortBy);
        var petId = await CreatePetAsync("SortPet " + sortBy, petTypeId);
        await AddFavoriteAsync(petId);

        // Act
        var response = await _client.GetAsync($"/api/favorites?sortBy={sortBy}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFavorites_SortByNewest_ReturnsNewestFirst()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("newestsort", "NewestSort");
        var firstPetId = await CreatePetAsync("FirstFavoritedPet", petTypeId);
        var secondPetId = await CreatePetAsync("SecondFavoritedPet", petTypeId);
        await AddFavoriteAsync(firstPetId);
        await AddFavoriteAsync(secondPetId);

        // Act
        var response = await _client.GetAsync("/api/favorites?sortBy=newest&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        var items = body!.Items.ToList();
        items.Should().HaveCountGreaterThanOrEqualTo(2);
        items.First().CreatedAt.Should().BeOnOrAfter(items.Last().CreatedAt);
    }

    // ──────────────────────────────────────────────────────────────
    // Filtering by PetType
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFavorites_FilterByPetTypeId_ReturnsOnlyMatchingType()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("dogfilter", "DogFilter");
        var catTypeId = await SeedPetTypeAsync("catfilter", "CatFilter");
        var dogPetId = await CreatePetAsync("FilterDog", dogTypeId);
        var catPetId = await CreatePetAsync("FilterCat", catTypeId);
        await AddFavoriteAsync(dogPetId);
        await AddFavoriteAsync(catPetId);

        // Act
        var response = await _client.GetAsync($"/api/favorites?petTypeId={dogTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        body!.Items.Should().OnlyContain(f => f.PetId == dogPetId);
    }

    // ──────────────────────────────────────────────────────────────
    // Check Favorite
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckFavorite_WhenFavorited_ReturnsTrue()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("checkfav", "CheckFav");
        var petId = await CreatePetAsync("CheckFavPet", petTypeId);
        await AddFavoriteAsync(petId);

        // Act
        var response = await _client.GetFromJsonAsync<CheckFavoriteResultDto>($"/api/favorites/check/{petId}");

        // Assert
        response.Should().NotBeNull();
        response!.IsFavorited.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFavorite_WhenNotFavorited_ReturnsFalse()
    {
        // Arrange
        var randomPetId = Guid.NewGuid();

        // Act
        var response = await _client.GetFromJsonAsync<CheckFavoriteResultDto>($"/api/favorites/check/{randomPetId}");

        // Assert
        response.Should().NotBeNull();
        response!.IsFavorited.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFavorite_AfterRemove_ReturnsFalse()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("checkremove", "CheckRemove");
        var petId = await CreatePetAsync("CheckRemovePet", petTypeId);
        await AddFavoriteAsync(petId);
        await _client.DeleteAsync($"/api/favorites/{petId}");

        // Act
        var response = await _client.GetFromJsonAsync<CheckFavoriteResultDto>($"/api/favorites/check/{petId}");

        // Assert
        response.Should().NotBeNull();
        response!.IsFavorited.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record FavoriteItemDto(Guid FavoriteId, Guid PetId, string PetName, string PetType, string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
    private record GetFavoritesResponseDto(List<FavoriteItemDto> Items, long TotalCount, int Page, int PageSize);
    private record CheckFavoriteResultDto(bool IsFavorited);
}
