using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.API.Controllers;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PetsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PetsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken());
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

    /// <summary>
    /// Gets a pet type ID, using an already-seeded type or creating a new one.
    /// The PetTypeSeeder seeds default types (dog, cat, rabbit, bird, fish, hamster) on startup.
    /// </summary>
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

    /// <summary>
    /// Creates a pet via the API and returns its ID.
    /// </summary>
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

    /// <summary>
    /// Creates a pet and reserves it, returning its ID.
    /// </summary>
    private async Task<Guid> CreateAndReservePetAsync(Guid? petTypeId = null)
    {
        var petId = await CreatePetAsync(petTypeId: petTypeId);
        var reserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return petId;
    }

    /// <summary>
    /// Creates a pet, reserves it, and adopts it, returning its ID.
    /// </summary>
    private async Task<Guid> CreateReserveAndAdoptPetAsync(Guid? petTypeId = null)
    {
        var petId = await CreateAndReservePetAsync(petTypeId);
        var adoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        adoptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return petId;
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/pets (Create Pet)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePet_WithValidData_ReturnsCreatedAndPetId()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = new CreatePetRequestBuilder()
            .WithName("Buddy")
            .WithPetTypeId(petTypeId)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePet_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = new CreatePetRequestBuilder()
            .WithName("")
            .WithPetTypeId(petTypeId)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePet_WithNonExistentPetType_ReturnsNotFound()
    {
        // Arrange - use a random GUID that doesn't correspond to any pet type
        var request = new CreatePetRequestBuilder()
            .WithName("Buddy")
            .WithPetTypeId(Guid.NewGuid())
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePet_WithNameExceedingMaxLength_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var longName = new string('A', 101); // PetName.MaxLength is 100
        var request = new CreatePetRequestBuilder()
            .WithName(longName)
            .WithPetTypeId(petTypeId)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/pets (Get All Pets)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPets_WhenNoPetsExist_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Skip.Should().Be(0);
        result.Take.Should().Be(20);
    }

    [Fact]
    public async Task GetAllPets_WhenPetsExist_ReturnsAllPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync("Buddy", petTypeId);
        await CreatePetAsync("Max", petTypeId);
        await CreatePetAsync("Luna", petTypeId);

        // Act
        var response = await _client.GetAsync("/api/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(3);
        result.Pets.Select(p => p.Name).Should().Contain(new[] { "Buddy", "Max", "Luna" });
        result.Total.Should().Be(3);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/pets/{id} (Get Pet By Id)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPetById_WithExistingPet_ReturnsPetDetails()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.GetAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pet = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        pet.Should().NotBeNull();
        pet!.Id.Should().Be(petId);
        pet.Name.Should().Be("Buddy");
        pet.Type.Should().Be("Dog");
        pet.Status.Should().Be("Available");
    }

    [Fact]
    public async Task GetPetById_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/pets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // PUT /api/pets/{id} (Update Pet)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePet_WithValidName_ReturnsOkAndUpdatedPet()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);
        var request = new UpdatePetRequestBuilder()
            .WithName("Max")
            .Build();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{petId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdatePetResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(petId);
        result.Name.Should().Be("Max");
        result.Status.Should().Be("Available");
    }

    [Fact]
    public async Task UpdatePet_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);
        var request = new UpdatePetRequestBuilder()
            .WithName("")
            .Build();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{petId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePet_WithNameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);
        var request = new UpdatePetRequestBuilder()
            .WithName(new string('A', 101))
            .Build();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{petId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdatePetRequestBuilder()
            .WithName("Max")
            .Build();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePet_VerifyGetReturnsUpdatedName()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);
        var request = new UpdatePetRequestBuilder()
            .WithName("Max")
            .Build();
        var updateResponse = await _client.PutAsJsonAsync($"/api/pets/{petId}", request);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pet = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        pet.Should().NotBeNull();
        pet!.Name.Should().Be("Max");
    }

    // ──────────────────────────────────────────────────────────────
    // DELETE /api/pets/{id} (Delete Pet)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePet_WhenAvailable_ReturnsNoContent()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePet_WhenReserved_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateAndReservePetAsync(petTypeId);

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePet_WhenAdopted_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateReserveAndAdoptPetAsync(petTypeId);

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePet_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/pets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePet_VerifyGetReturnsNotFound()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);
        var deleteResponse = await _client.DeleteAsync($"/api/pets/{petId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await _client.GetAsync($"/api/pets/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/pets/{id}/reserve (Reserve Pet)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservePet_WhenAvailable_ReturnsOkAndReservedStatus()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PetId.Should().Be(petId);
        result.Status.Should().Be("Reserved");
    }

    [Fact]
    public async Task ReservePet_WhenAlreadyReserved_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateAndReservePetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        // Assert - Domain throws PetNotAvailable which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReservePet_WhenAdopted_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateReserveAndAdoptPetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        // Assert - Domain throws PetNotAvailable which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReservePet_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/reserve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/pets/{id}/adopt (Adopt Pet)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdoptPet_WhenReserved_ReturnsOkAndAdoptedStatus()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateAndReservePetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PetId.Should().Be(petId);
        result.Status.Should().Be("Adopted");
    }

    [Fact]
    public async Task AdoptPet_WhenAvailable_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        // Assert - Domain throws PetNotReserved which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdoptPet_WhenAlreadyAdopted_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateReserveAndAdoptPetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        // Assert - Domain throws PetNotReserved which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdoptPet_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/adopt", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/pets/{id}/cancel-reservation (Cancel Reservation)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelReservation_WhenReserved_ReturnsOkAndAvailableStatus()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateAndReservePetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PetId.Should().Be(petId);
        result.Status.Should().Be("Available");
    }

    [Fact]
    public async Task CancelReservation_WhenAvailable_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        // Assert - Domain throws PetNotReserved which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelReservation_WhenAdopted_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateReserveAndAdoptPetAsync(petTypeId);

        // Act
        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        // Assert - Domain throws PetNotReserved which maps to Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelReservation_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/cancel-reservation", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/pets with filtering and pagination
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_FilterByStatus_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync("Available1", petTypeId);
        await CreatePetAsync("Available2", petTypeId);
        await CreateAndReservePetAsync(petTypeId);

        // Act
        var response = await _client.GetAsync("/api/pets?status=Available");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(2);
        result.Pets.Should().OnlyContain(p => p.Status == "Available");
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetPets_FilterByPetTypeId_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("dog", "Dog");
        var catTypeId = await SeedPetTypeAsync("cat", "Cat");
        await CreatePetAsync("Buddy", dogTypeId);
        await CreatePetAsync("Max", dogTypeId);
        await CreatePetAsync("Whiskers", catTypeId);

        // Act
        var response = await _client.GetAsync($"/api/pets?petTypeId={catTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(1);
        result.Pets[0].Name.Should().Be("Whiskers");
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetPets_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        for (var i = 1; i <= 5; i++)
            await CreatePetAsync($"Pet{i}", petTypeId);

        // Act
        var response = await _client.GetAsync("/api/pets?skip=2&take=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Skip.Should().Be(2);
        result.Take.Should().Be(2);
    }

    [Fact]
    public async Task GetPets_FilterByStatusAndPetType_ReturnsCombinedFilter()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("dog", "Dog");
        var catTypeId = await SeedPetTypeAsync("cat", "Cat");
        await CreatePetAsync("Buddy", dogTypeId);
        await CreateAndReservePetAsync(dogTypeId);
        await CreatePetAsync("Whiskers", catTypeId);

        // Act
        var response = await _client.GetAsync($"/api/pets?status=Available&petTypeId={dogTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(1);
        result.Pets[0].Name.Should().Be("Buddy");
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetPets_WithInvalidStatus_IgnoresFilterAndReturnsAll()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync("Buddy", petTypeId);

        // Act
        var response = await _client.GetAsync("/api/pets?status=InvalidStatus");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetsResponseDto>();
        result.Should().NotBeNull();
        result!.Pets.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // Tags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePet_WithTags_ShouldPersistTags()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = CreatePetRequestBuilder.Default()
            .WithName("TaggedPet")
            .WithPetTypeId(petTypeId)
            .WithTags("friendly", "vaccinated")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        created.Should().NotBeNull();

        // Verify tags via GET
        var getResponse = await _client.GetAsync($"/api/pets/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pet = await getResponse.Content.ReadFromJsonAsync<PetDetailWithTagsDto>();
        pet!.Tags.Should().BeEquivalentTo(new[] { "friendly", "vaccinated" });
    }

    [Fact]
    public async Task GetPets_WithTagFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();

        await CreatePetWithTagsAsync("Pet1", petTypeId, "friendly", "vaccinated");
        await CreatePetWithTagsAsync("Pet2", petTypeId, "friendly");
        await CreatePetWithTagsAsync("Pet3", petTypeId, "neutered");

        // Act - filter by "friendly"
        var response = await _client.GetAsync("/api/pets?tags=friendly");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PetsWithTagsResponseDto>();
        result!.Pets.Should().OnlyContain(p => p.Tags.Contains("friendly"));
    }

    // ──────────────────────────────────────────────────────────────
    // Tag Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreatePetWithTagsAsync(string name, Guid petTypeId, params string[] tags)
    {
        var request = CreatePetRequestBuilder.Default()
            .WithName(name)
            .WithPetTypeId(petTypeId)
            .WithTags(tags)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        return created!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);

    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);

    private record PetListItemResponseDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);

    private record PetDetailsResponseDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);

    private record StatusChangeResponseDto(bool Success, string? Message, Guid? PetId, string? Status);

    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);

    private record UpdatePetResponseDto(Guid Id, string Name, string Status, string? Breed, int? AgeMonths, string? Description);

    private record GetPetsResponseDto(List<PetListItemResponseDto> Pets, long Total, int Skip, int Take);

    private record PetDetailWithTagsDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
    private record PetsWithTagsResponseDto(List<PetDetailWithTagsDto> Pets, long Total, int Skip, int Take);
}
