using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

internal class FavoritesEnhancedTests : IntegrationTestBase
{
    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public FavoritesEnhancedTests(SqlServerFixture sqlFixture) : base(sqlFixture) { }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: TestUserId));
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

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
        var petTypeId = await SeedPetTypeAsync($"sorttest{sortBy}", "SortTest " + sortBy);
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

    [Fact]
    public async Task GetFavorites_SortByOldest_ReturnsOldestFirst()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("oldestsort", "OldestSort");
        var pet1Id = await CreatePetAsync("OldestSortPet1", petTypeId);
        var pet2Id = await CreatePetAsync("OldestSortPet2", petTypeId);
        var pet3Id = await CreatePetAsync("OldestSortPet3", petTypeId);
        await AddFavoriteAsync(pet1Id);
        await AddFavoriteAsync(pet2Id);
        await AddFavoriteAsync(pet3Id);

        // Act
        var response = await _client.GetAsync("/api/favorites?sortBy=oldest&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        var items = body!.Items
            .Where(i => i.PetId == pet1Id || i.PetId == pet2Id || i.PetId == pet3Id)
            .ToList();
        items.Should().HaveCount(3);
        items.First().CreatedAt.Should().BeOnOrBefore(items.Last().CreatedAt);
    }

    [Fact]
    public async Task GetFavorites_SortByName_ReturnsAlphabetical()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("namesort", "NameSort");
        var charliePetId = await CreatePetAsync("Charlie", petTypeId);
        var alphaPetId = await CreatePetAsync("Alpha", petTypeId);
        var bravoPetId = await CreatePetAsync("Bravo", petTypeId);
        await AddFavoriteAsync(charliePetId);
        await AddFavoriteAsync(alphaPetId);
        await AddFavoriteAsync(bravoPetId);

        // Act
        var response = await _client.GetAsync("/api/favorites?sortBy=name&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        var items = body!.Items
            .Where(i => i.PetId == charliePetId || i.PetId == alphaPetId || i.PetId == bravoPetId)
            .ToList();
        items.Should().HaveCount(3);
        var names = items.Select(i => i.PetName).ToList();
        names.Should().BeInAscendingOrder();
        names[0].Should().Be("Alpha");
        names[1].Should().Be("Bravo");
        names[2].Should().Be("Charlie");
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
    // Filtering by Status
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFavorites_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("statusfilter", "StatusFilter");
        var availablePetId = await CreatePetAsync("StatusAvailablePet", petTypeId);
        var reservedPetId = await CreatePetAsync("StatusReservedPet", petTypeId);
        await AddFavoriteAsync(availablePetId);
        await AddFavoriteAsync(reservedPetId);
        await _client.PostAsync($"/api/pets/{reservedPetId}/reserve", null);

        // Act
        var response = await _client.GetAsync("/api/favorites?status=Available");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        var ownItems = body!.Items
            .Where(i => i.PetId == availablePetId || i.PetId == reservedPetId)
            .ToList();
        ownItems.Should().NotBeEmpty();
        ownItems.Should().OnlyContain(i => i.Status == "Available");
    }

    // ──────────────────────────────────────────────────────────────
    // Combined Filter and Sort
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFavorites_CombinedFilterAndSort_AppliesBoth()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("combineddog", "CombinedDog");
        var catTypeId = await SeedPetTypeAsync("combinedcat", "CombinedCat");
        var dogPet1Id = await CreatePetAsync("CombinedDog1", dogTypeId);
        var dogPet2Id = await CreatePetAsync("CombinedDog2", dogTypeId);
        var dogPet3Id = await CreatePetAsync("CombinedDog3", dogTypeId);
        var catPetId = await CreatePetAsync("CombinedCat1", catTypeId);
        await AddFavoriteAsync(dogPet1Id);
        await AddFavoriteAsync(catPetId);
        await AddFavoriteAsync(dogPet2Id);
        await AddFavoriteAsync(dogPet3Id);

        // Act
        var response = await _client.GetAsync($"/api/favorites?petTypeId={dogTypeId}&sortBy=newest&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetFavoritesResponseDto>();
        body.Should().NotBeNull();
        var items = body!.Items
            .Where(i => i.PetId == dogPet1Id || i.PetId == dogPet2Id || i.PetId == dogPet3Id || i.PetId == catPetId)
            .ToList();
        items.Should().HaveCount(3);
        items.Should().NotContain(i => i.PetId == catPetId);
        items.First().CreatedAt.Should().BeOnOrAfter(items.Last().CreatedAt);
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
    private record FavoriteItemDto(Guid FavoriteId, Guid PetId, string PetName, string PetType, string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
    private record GetFavoritesResponseDto(List<FavoriteItemDto> Items, long TotalCount, int Page, int PageSize);
    private record CheckFavoriteResultDto(bool IsFavorited);
}
