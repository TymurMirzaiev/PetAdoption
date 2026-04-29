using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.API.Controllers;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class PetTypesAdminControllerTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PetTypesAdminControllerTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_mongoFixture.ConnectionString);
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
    /// Seeds a pet type via the admin API and returns its ID.
    /// </summary>
    private async Task<Guid> SeedPetTypeAsync(string code, string name)
    {
        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName(name)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
        result.Should().NotBeNull();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/admin/pet-types (Create Pet Type)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePetType_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreatePetTypeRequestBuilder()
            .WithCode("gecko_create")
            .WithName("Gecko")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Code.Should().Be("gecko_create");
        result.Name.Should().Be("Gecko");
    }

    [Fact]
    public async Task CreatePetType_WithDuplicateCode_ReturnsConflict()
    {
        // Arrange - create a pet type first
        var code = "iguana_dup";
        await SeedPetTypeAsync(code, "Iguana");

        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName("Another Iguana")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePetType_WithEmptyCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePetTypeRequestBuilder()
            .WithCode("")
            .WithName("Empty Code Type")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePetType_WithCodeTooShort_ReturnsBadRequest()
    {
        // Arrange - 1 char code, min is 2
        var request = new CreatePetTypeRequestBuilder()
            .WithCode("x")
            .WithName("Too Short Code")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePetType_WithCodeTooLong_ReturnsBadRequest()
    {
        // Arrange - >50 chars code
        var longCode = new string('a', 51);
        var request = new CreatePetTypeRequestBuilder()
            .WithCode(longCode)
            .WithName("Too Long Code")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/admin/pet-types (Get All Pet Types)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPetTypes_ReturnsOnlyActiveByDefault()
    {
        // Arrange - create an active and a deactivated pet type
        var activeId = await SeedPetTypeAsync("parrot_active", "Parrot Active");
        var inactiveId = await SeedPetTypeAsync("parrot_inactive", "Parrot Inactive");

        // Deactivate one
        var deactivateResponse = await _client.PostAsync($"/api/admin/pet-types/{inactiveId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync("/api/admin/pet-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var petTypes = await response.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        petTypes.Should().NotBeNull();
        petTypes!.Should().OnlyContain(pt => pt.IsActive);
        petTypes.Should().Contain(pt => pt.Id == activeId);
        petTypes.Should().NotContain(pt => pt.Id == inactiveId);
    }

    [Fact]
    public async Task GetAllPetTypes_WithIncludeInactive_ReturnsAll()
    {
        // Arrange - create an active and a deactivated pet type
        var activeId = await SeedPetTypeAsync("turtle_active", "Turtle Active");
        var inactiveId = await SeedPetTypeAsync("turtle_inactive", "Turtle Inactive");

        // Deactivate one
        var deactivateResponse = await _client.PostAsync($"/api/admin/pet-types/{inactiveId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var petTypes = await response.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        petTypes.Should().NotBeNull();
        petTypes!.Should().Contain(pt => pt.Id == activeId);
        petTypes.Should().Contain(pt => pt.Id == inactiveId);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/admin/pet-types/{id} (Get Pet Type By Id)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPetTypeById_WithExistingId_ReturnsPetType()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("snake_get", "Snake");

        // Act
        var response = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(petTypeId);
        result.Code.Should().Be("snake_get");
        result.Name.Should().Be("Snake");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetPetTypeById_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/admin/pet-types/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // PUT /api/admin/pet-types/{id} (Update Pet Type)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePetType_WithValidName_ReturnsOk()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("lizard_upd", "Lizard");
        var updateRequest = new UpdatePetTypeRequest("Updated Lizard");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/pet-types/{petTypeId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the name was updated
        var getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        var result = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Lizard");
    }

    [Fact]
    public async Task UpdatePetType_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdatePetTypeRequest("Some Name");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/pet-types/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/admin/pet-types/{id}/deactivate (Deactivate Pet Type)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivatePetType_WhenActive_ReturnsOk()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("frog_deact", "Frog");

        // Act
        var response = await _client.PostAsync($"/api/admin/pet-types/{petTypeId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the pet type is deactivated
        var getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        var result = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivatePetType_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/admin/pet-types/{Guid.NewGuid()}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/admin/pet-types/{id}/activate (Activate Pet Type)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivatePetType_WhenInactive_ReturnsOk()
    {
        // Arrange - create and deactivate a pet type
        var petTypeId = await SeedPetTypeAsync("newt_act", "Newt");
        var deactivateResponse = await _client.PostAsync($"/api/admin/pet-types/{petTypeId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.PostAsync($"/api/admin/pet-types/{petTypeId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the pet type is active again
        var getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        var result = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivatePetType_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/admin/pet-types/{Guid.NewGuid()}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);

    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
}
