using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Helpers;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

[Collection("SqlServer")]
internal abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly SqlServerFixture _sqlFixture;
    protected PetServiceWebAppFactory _factory = null!;
    protected HttpClient _client = null!;

    protected IntegrationTestBase(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public virtual Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a pet type via the admin API and returns its ID.
    /// Uses a fresh admin client so subclasses need not configure _client as Admin.
    /// If the type already exists (409 Conflict), looks it up and returns its ID.
    /// </summary>
    protected async Task<Guid> SeedPetTypeAsync(string code = "dog", string name = "Dog")
    {
        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken());

        var request = new CreatePetTypeRequestBuilder()
            .WithCode(code)
            .WithName(name)
            .Build();

        var response = await adminClient.PostAsJsonAsync("/api/admin/pet-types", request);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var allTypesResponse = await adminClient.GetAsync("/api/admin/pet-types?includeInactive=true");
        allTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        return allTypes!.First(t => t.Code == code).Id;
    }

    /// <summary>
    /// Seeds a pet directly in the database with the given OrganizationId.
    /// The API does not expose the OrganizationId field, so direct DB insertion is required.
    /// </summary>
    protected async Task<Pet> SeedPetWithOrgAsync(Guid orgId, string? name = null, Guid? petTypeId = null)
    {
        var typeId = petTypeId ?? await SeedPetTypeAsync();
        var petName = name ?? $"Pet_{Guid.NewGuid():N}"[..20];

        await using var db = _factory.CreateDbContext();
        var pet = Pet.Create(petName, typeId, breed: null, ageMonths: 24, description: null);
        pet.AssignToOrganization(orgId);
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        return pet;
    }
}
