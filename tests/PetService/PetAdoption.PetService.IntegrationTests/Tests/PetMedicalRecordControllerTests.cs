using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PetMedicalRecordControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public PetMedicalRecordControllerTests(SqlServerFixture sqlFixture)
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
    // PUT medical-record
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMedicalRecord_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var request = new
        {
            IsSpayedNeutered = true,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = (string?)null,
            HistoryNotes = (string?)null,
            LastVetVisit = (DateOnly?)null,
            Vaccinations = new[] { new { VaccineType = "Rabies", AdministeredOn = "2024-01-15", NextDueOn = (string?)null, Notes = (string?)null } },
            Allergies = new[] { "pollen" }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateMedicalRecordResponseDto>();
        result!.PetId.Should().Be(petId);
    }

    [Fact]
    public async Task UpdateMedicalRecord_ThenGetPet_ReturnsMedicalInfo()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var request = new
        {
            IsSpayedNeutered = false,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = "ABCD1234",
            HistoryNotes = "Regular checkup done.",
            LastVetVisit = (DateOnly?)null,
            Vaccinations = Array.Empty<object>(),
            Allergies = new[] { "dust" }
        };
        await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", request);

        // Act — get pet publicly (anonymous)
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetAsync($"/api/pets/{petId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pet = await getResponse.Content.ReadFromJsonAsync<PetDetailsDto>();
        pet!.MedicalRecord.Should().NotBeNull();
        pet.MedicalRecord!.MicrochipId.Should().Be("ABCD1234");
        pet.MedicalRecord.Allergies.Should().Contain("dust");
    }

    [Fact]
    public async Task UpdateMedicalRecord_WithInvalidMicrochipId_ReturnsBadRequest()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var request = new
        {
            IsSpayedNeutered = false,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = "SHORT",  // too short — less than 8 chars
            HistoryNotes = (string?)null,
            LastVetVisit = (DateOnly?)null,
            Vaccinations = Array.Empty<object>(),
            Allergies = Array.Empty<string>()
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMedicalRecord_FromWrongOrg_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);

        var wrongOrgClient = _factory.CreateClient();
        var wrongOrgId = Guid.NewGuid();
        wrongOrgClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "other-org-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", wrongOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        var request = new
        {
            IsSpayedNeutered = true,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = (string?)null,
            HistoryNotes = (string?)null,
            LastVetVisit = (DateOnly?)null,
            Vaccinations = Array.Empty<object>(),
            Allergies = Array.Empty<string>()
        };

        // Act — wrong org tries to update medical record of another org's pet
        var response = await wrongOrgClient.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateMedicalRecord_Idempotent_CanUpdateTwice()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);

        var firstRequest = new
        {
            IsSpayedNeutered = false,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = (string?)null,
            HistoryNotes = (string?)null,
            LastVetVisit = (DateOnly?)null,
            Vaccinations = Array.Empty<object>(),
            Allergies = new[] { "dust" }
        };
        await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", firstRequest);

        var secondRequest = new
        {
            IsSpayedNeutered = true,
            SpayNeuterDate = (DateOnly?)null,
            MicrochipId = (string?)null,
            HistoryNotes = (string?)null,
            LastVetVisit = (DateOnly?)null,
            Vaccinations = Array.Empty<object>(),
            Allergies = new[] { "pollen" }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/medical-record", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private async Task<Guid> CreateOrgPetAsync(Guid petTypeId)
    {
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("MedPet")
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrgPetResponseDto>();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record UpdateMedicalRecordResponseDto(Guid PetId, DateTime UpdatedAt);
    private record CreatePetTypeResponseDto(Guid Id);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record CreateOrgPetResponseDto(Guid Id, string Name);
    private record PetDetailsDto(Guid Id, string Name, string Type, string Status, MedicalRecordDto? MedicalRecord);
    private record MedicalRecordDto(bool IsSpayedNeutered, string? MicrochipId, List<string> Allergies);
}
