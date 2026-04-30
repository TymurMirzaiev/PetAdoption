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
public class PetWorkflowTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PetWorkflowTests(SqlServerFixture sqlFixture)
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

    /// <summary>
    /// Creates a pet via the API and returns its ID.
    /// </summary>
    private async Task<Guid> CreatePetAsync(string name, Guid petTypeId)
    {
        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        result.Should().NotBeNull();
        return result!.Id;
    }

    /// <summary>
    /// Gets a pet by ID and returns its details.
    /// </summary>
    private async Task<PetDetailsResponseDto> GetPetAsync(Guid petId)
    {
        var response = await _client.GetAsync($"/api/pets/{petId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        result.Should().NotBeNull();
        return result!;
    }

    // ──────────────────────────────────────────────────────────────
    // Full Lifecycle Workflow Tests
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullAdoptionWorkflow_CreateReserveAdopt_Success()
    {
        // Arrange - create a pet type and a pet
        var petTypeId = await SeedPetTypeAsync("wf_adopt", "Workflow Adopt");
        var petId = await CreatePetAsync("Buddy Workflow", petTypeId);

        // Verify initial state is Available
        var pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Available");

        // Act - Reserve the pet
        var reserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reserveResult = await reserveResponse.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        reserveResult.Should().NotBeNull();
        reserveResult!.Success.Should().BeTrue();
        reserveResult.Status.Should().Be("Reserved");

        // Verify reserved state
        pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Reserved");

        // Act - Adopt the pet
        var adoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        adoptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adoptResult = await adoptResponse.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        adoptResult.Should().NotBeNull();
        adoptResult!.Success.Should().BeTrue();
        adoptResult.Status.Should().Be("Adopted");

        // Verify adopted state
        pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Adopted");

        // Verify cannot reserve an adopted pet
        var reReserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reReserveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Verify cannot adopt an already adopted pet
        var reAdoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        reAdoptResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReserveCancelReReserveAdopt_Success()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("wf_cancel", "Workflow Cancel");
        var petId = await CreatePetAsync("Max Workflow", petTypeId);

        // Reserve the pet
        var reserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Reserved");

        // Cancel the reservation
        var cancelResponse = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelResult = await cancelResponse.Content.ReadFromJsonAsync<StatusChangeResponseDto>();
        cancelResult.Should().NotBeNull();
        cancelResult!.Success.Should().BeTrue();
        cancelResult.Status.Should().Be("Available");

        // Verify back to Available
        pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Available");

        // Re-reserve the pet
        var reReserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reReserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Reserved");

        // Adopt the pet
        var adoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        adoptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        pet = await GetPetAsync(petId);
        pet.Status.Should().Be("Adopted");
    }

    [Fact]
    public async Task MultiplePets_IndependentStateTransitions()
    {
        // Arrange - create a pet type and multiple pets
        var petTypeId = await SeedPetTypeAsync("wf_multi", "Workflow Multi");
        var pet1Id = await CreatePetAsync("Pet One", petTypeId);
        var pet2Id = await CreatePetAsync("Pet Two", petTypeId);
        var pet3Id = await CreatePetAsync("Pet Three", petTypeId);

        // Reserve pet1 only
        var reserve1Response = await _client.PostAsync($"/api/pets/{pet1Id}/reserve", null);
        reserve1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reserve and adopt pet2
        var reserve2Response = await _client.PostAsync($"/api/pets/{pet2Id}/reserve", null);
        reserve2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var adopt2Response = await _client.PostAsync($"/api/pets/{pet2Id}/adopt", null);
        adopt2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Leave pet3 as Available

        // Assert - each pet has its own independent state
        var pet1 = await GetPetAsync(pet1Id);
        pet1.Status.Should().Be("Reserved");

        var pet2 = await GetPetAsync(pet2Id);
        pet2.Status.Should().Be("Adopted");

        var pet3 = await GetPetAsync(pet3Id);
        pet3.Status.Should().Be("Available");

        // Verify pet3 can still be reserved independently
        var reserve3Response = await _client.PostAsync($"/api/pets/{pet3Id}/reserve", null);
        reserve3Response.StatusCode.Should().Be(HttpStatusCode.OK);

        pet3 = await GetPetAsync(pet3Id);
        pet3.Status.Should().Be("Reserved");
    }

    [Fact]
    public async Task PetType_FullLifecycle_CreateUpdateDeactivateActivate()
    {
        // Create a pet type
        var createRequest = new CreatePetTypeRequestBuilder()
            .WithCode("wf_lifecycle")
            .WithName("Lifecycle Type")
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/admin/pet-types", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
        created.Should().NotBeNull();
        var petTypeId = created!.Id;

        // Verify created state
        var getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var petType = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        petType.Should().NotBeNull();
        petType!.Code.Should().Be("wf_lifecycle");
        petType.Name.Should().Be("Lifecycle Type");
        petType.IsActive.Should().BeTrue();

        // Update the name
        var updateRequest = new UpdatePetTypeRequest("Updated Lifecycle Type");
        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/pet-types/{petTypeId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify updated
        getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        petType = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        petType.Should().NotBeNull();
        petType!.Name.Should().Be("Updated Lifecycle Type");
        petType.IsActive.Should().BeTrue();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/admin/pet-types/{petTypeId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deactivated
        getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        petType = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        petType.Should().NotBeNull();
        petType!.IsActive.Should().BeFalse();

        // Should not appear in active-only list
        var activeListResponse = await _client.GetAsync("/api/admin/pet-types");
        var activeList = await activeListResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        activeList.Should().NotBeNull();
        activeList!.Should().NotContain(pt => pt.Id == petTypeId);

        // Activate again
        var activateResponse = await _client.PostAsync($"/api/admin/pet-types/{petTypeId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify reactivated
        getResponse = await _client.GetAsync($"/api/admin/pet-types/{petTypeId}");
        petType = await getResponse.Content.ReadFromJsonAsync<PetTypeResponseDto>();
        petType.Should().NotBeNull();
        petType!.IsActive.Should().BeTrue();
        petType.Name.Should().Be("Updated Lifecycle Type");
    }

    [Fact]
    public async Task CreatePetWithSeededType_Success()
    {
        // The PetTypeSeeder seeds default types: "dog", "cat", "rabbit", "bird", "fish", "hamster"
        // Get all pet types to find the seeded "dog" type
        var response = await _client.GetAsync("/api/admin/pet-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var petTypes = await response.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        petTypes.Should().NotBeNull();

        var dogType = petTypes!.FirstOrDefault(pt => pt.Code == "dog");
        dogType.Should().NotBeNull("The 'dog' pet type should be seeded on startup");

        // Create a pet using the seeded dog type
        var petId = await CreatePetAsync("Seeded Dog Pet", dogType!.Id);

        // Verify the pet was created with the correct type
        var pet = await GetPetAsync(petId);
        pet.Name.Should().Be("Seeded Dog Pet");
        pet.Type.Should().Be("Dog");
        pet.Status.Should().Be("Available");
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);

    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);

    private record PetDetailsResponseDto(Guid Id, string Name, string Type, string Status);

    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

    private record StatusChangeResponseDto(bool Success, string? Message, Guid? PetId, string? Status);
}
