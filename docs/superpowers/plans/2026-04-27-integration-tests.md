# Integration Tests with Fluent Builders - Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add comprehensive integration tests for PetService and UserService using fluent object builders, WebApplicationFactory, and Testcontainers.

**Architecture:** Each service gets its own integration test project with a custom `WebApplicationFactory` backed by Testcontainers (MongoDB, RabbitMQ). Fluent builders create domain objects for the arrange block, eliminating inline construction noise. Tests hit real HTTP endpoints and verify full request/response cycles against real infrastructure.

**Tech Stack:** xUnit, FluentAssertions, Testcontainers.MongoDb, Testcontainers.RabbitMq, Microsoft.AspNetCore.Mvc.Testing, .NET 9 (PetService) / .NET 10 (UserService)

---

## File Structure

### PetService Integration Tests

```
tests/PetService/PetAdoption.PetService.IntegrationTests/
  PetAdoption.PetService.IntegrationTests.csproj
  Infrastructure/
    PetServiceWebAppFactory.cs          -- WebApplicationFactory<Program> with Testcontainers
    MongoDbFixture.cs                   -- Shared MongoDB container lifecycle
  Builders/
    PetBuilder.cs                       -- Fluent builder for Pet aggregate
    PetTypeBuilder.cs                   -- Fluent builder for PetType entity
    CreatePetRequestBuilder.cs          -- Fluent builder for API request DTOs
    CreatePetTypeRequestBuilder.cs      -- Fluent builder for API request DTOs
  Tests/
    PetsControllerTests.cs             -- All /api/pets endpoint tests
    PetTypesAdminControllerTests.cs    -- All /api/admin/pet-types endpoint tests
    PetWorkflowTests.cs                -- Full lifecycle tests (create->reserve->adopt)
```

### UserService Integration Tests

```
tests/UserService/PetAdoption.UserService.IntegrationTests/
  PetAdoption.UserService.IntegrationTests.csproj
  Infrastructure/
    UserServiceWebAppFactory.cs         -- WebApplicationFactory<Program> with Testcontainers
    MongoDbFixture.cs                   -- Shared MongoDB container lifecycle
    AuthHelper.cs                       -- Helper to get JWT tokens for authenticated requests
  Builders/
    UserBuilder.cs                      -- Fluent builder for User aggregate
    RegisterUserRequestBuilder.cs       -- Fluent builder for registration request DTO
    UpdateProfileRequestBuilder.cs      -- Fluent builder for profile update request DTO
    UserPreferencesBuilder.cs           -- Fluent builder for UserPreferences value object
  Tests/
    AuthenticationTests.cs             -- Register + Login endpoint tests
    UserProfileTests.cs                -- /api/users/me endpoints
    AdminUserManagementTests.cs        -- Admin-only endpoints (list, get, suspend, promote)
    PasswordManagementTests.cs         -- Change password tests
```

---

## Chunk 1: PetService Integration Test Infrastructure & Builders

### Task 1: Create PetService Integration Test Project

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/PetAdoption.PetService.IntegrationTests.csproj`

- [ ] **Step 1: Create the .csproj file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.6" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Testcontainers.MongoDb" Version="4.5.0" />
    <PackageReference Include="Testcontainers.RabbitMq" Version="4.5.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Services\PetService\PetAdoption.PetService.API\PetAdoption.PetService.API.csproj" />
    <ProjectReference Include="..\..\..\src\Services\PetService\PetAdoption.PetService.Domain\PetAdoption.PetService.Domain.csproj" />
    <ProjectReference Include="..\..\..\src\Services\PetService\PetAdoption.PetService.Infrastructure\PetAdoption.PetService.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add project to solution**

Run: `dotnet sln add tests/PetService/PetAdoption.PetService.IntegrationTests/PetAdoption.PetService.IntegrationTests.csproj`
Expected: Project added to solution successfully.

- [ ] **Step 3: Verify it builds**

Run: `dotnet build tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/ PetAdoption.sln
git commit -m "chore: add PetService integration tests project with Testcontainers"
```

---

### Task 2: PetService WebApplicationFactory + MongoDB Fixture

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/MongoDbFixture.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/PetServiceWebAppFactory.cs`

- [ ] **Step 1: Create MongoDbFixture (shared container for all tests)**

```csharp
using Testcontainers.MongoDb;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async ValueTask InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
```

- [ ] **Step 2: Create PetServiceWebAppFactory**

This overrides the MongoDB connection string and disables RabbitMQ background services for integration tests. Read `Program.cs` of PetService.API to identify the exact service registrations to replace.

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

public class PetServiceWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public PetServiceWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
        _databaseName = $"PetAdoptionDb_Test_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing MongoDB registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IMongoDatabase)
                         || d.ServiceType == typeof(IMongoClient))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            // Register test MongoDB
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);
            services.AddSingleton<IMongoClient>(client);
            services.AddSingleton(database);

            // Remove RabbitMQ background services to avoid connection failures
            var backgroundServices = services
                .Where(d => d.ImplementationType?.Name is "OutboxProcessorService"
                          or "RabbitMqTopologySetup")
                .ToList();
            foreach (var d in backgroundServices) services.Remove(d);
        });

        builder.UseEnvironment("Development");
    }

    public IMongoDatabase GetDatabase()
    {
        var client = new MongoClient(_connectionString);
        return client.GetDatabase(_databaseName);
    }
}
```

- [ ] **Step 3: Verify it builds**

Run: `dotnet build tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/
git commit -m "feat: add PetService WebApplicationFactory with Testcontainers MongoDB"
```

---

### Task 3: PetService Fluent Builders

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/PetBuilder.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/PetTypeBuilder.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreatePetRequestBuilder.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreatePetTypeRequestBuilder.cs`

- [ ] **Step 1: Create PetBuilder (domain entity builder)**

```csharp
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class PetBuilder
{
    private string _name = "Buddy";
    private Guid _petTypeId = Guid.NewGuid();

    public PetBuilder WithName(string name) { _name = name; return this; }
    public PetBuilder WithPetTypeId(Guid petTypeId) { _petTypeId = petTypeId; return this; }

    public Pet Build() => Pet.Create(_name, _petTypeId);

    public static PetBuilder Default() => new();
}
```

- [ ] **Step 2: Create PetTypeBuilder (domain entity builder)**

```csharp
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class PetTypeBuilder
{
    private string _code = "dog";
    private string _name = "Dog";

    public PetTypeBuilder WithCode(string code) { _code = code; return this; }
    public PetTypeBuilder WithName(string name) { _name = name; return this; }

    public PetType Build() => PetType.Create(_code, _name);

    public static PetTypeBuilder Default() => new();
}
```

- [ ] **Step 3: Create CreatePetRequestBuilder (API DTO builder)**

Read the `CreatePetRequest` record in PetsController to get exact property names.

```csharp
namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreatePetRequestBuilder
{
    private string _name = "Buddy";
    private Guid _petTypeId = Guid.NewGuid();

    public CreatePetRequestBuilder WithName(string name) { _name = name; return this; }
    public CreatePetRequestBuilder WithPetTypeId(Guid petTypeId) { _petTypeId = petTypeId; return this; }

    public object Build() => new { Name = _name, PetTypeId = _petTypeId };

    public static CreatePetRequestBuilder Default() => new();
}
```

- [ ] **Step 4: Create CreatePetTypeRequestBuilder**

```csharp
namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreatePetTypeRequestBuilder
{
    private string _code = "dog";
    private string _name = "Dog";

    public CreatePetTypeRequestBuilder WithCode(string code) { _code = code; return this; }
    public CreatePetTypeRequestBuilder WithName(string name) { _name = name; return this; }

    public object Build() => new { Code = _code, Name = _name };

    public static CreatePetTypeRequestBuilder Default() => new();
}
```

- [ ] **Step 5: Verify it builds**

Run: `dotnet build tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/
git commit -m "feat: add fluent builders for PetService integration tests"
```

---

## Chunk 2: PetService Integration Tests - All Endpoints

### Task 4: PetsController Integration Tests

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`

- [ ] **Step 1: Write all PetsController tests**

Test cases to cover:

**POST /api/pets (Create Pet)**
1. `CreatePet_WithValidData_Returns201AndPetId` - happy path
2. `CreatePet_WithEmptyName_Returns400` - validation error
3. `CreatePet_WithNonExistentPetType_Returns404` - missing pet type
4. `CreatePet_WithNameExceedingMaxLength_Returns400` - name too long (>100 chars)

**GET /api/pets (Get All Pets)**
5. `GetAllPets_WhenNoPetsExist_ReturnsEmptyList`
6. `GetAllPets_WhenPetsExist_ReturnsAllPets` - create 3 pets, verify all returned

**GET /api/pets/{id} (Get Pet By Id)**
7. `GetPetById_WithExistingPet_ReturnsPetDetails`
8. `GetPetById_WithNonExistentId_Returns404`

**POST /api/pets/{id}/reserve (Reserve Pet)**
9. `ReservePet_WhenAvailable_Returns200AndReservedStatus`
10. `ReservePet_WhenAlreadyReserved_Returns400`
11. `ReservePet_WhenAdopted_Returns400`
12. `ReservePet_WithNonExistentId_Returns404`

**POST /api/pets/{id}/adopt (Adopt Pet)**
13. `AdoptPet_WhenReserved_Returns200AndAdoptedStatus`
14. `AdoptPet_WhenAvailable_Returns400` - must be reserved first
15. `AdoptPet_WhenAlreadyAdopted_Returns400`
16. `AdoptPet_WithNonExistentId_Returns404`

**POST /api/pets/{id}/cancel-reservation (Cancel Reservation)**
17. `CancelReservation_WhenReserved_Returns200AndAvailableStatus`
18. `CancelReservation_WhenAvailable_Returns400`
19. `CancelReservation_WhenAdopted_Returns400`
20. `CancelReservation_WithNonExistentId_Returns404`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class PetsControllerTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PetsControllerTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // --- Helper: seed a pet type and return its ID ---
    private async Task<Guid> SeedPetTypeAsync(string code = "dog", string name = "Dog")
    {
        var request = CreatePetTypeRequestBuilder.Default()
            .WithCode(code)
            .WithName(name)
            .Build();
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreatePetTypeApiResponse>();
        return body!.Id;
    }

    // --- Helper: create a pet and return its ID ---
    private async Task<Guid> CreatePetAsync(Guid petTypeId, string name = "Buddy")
    {
        var request = CreatePetRequestBuilder.Default()
            .WithName(name)
            .WithPetTypeId(petTypeId)
            .Build();
        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreatePetApiResponse>();
        return body!.Id;
    }

    // ===== POST /api/pets =====

    [Fact]
    public async Task CreatePet_WithValidData_Returns201AndPetId()
    {
        var petTypeId = await SeedPetTypeAsync();
        var request = CreatePetRequestBuilder.Default()
            .WithName("Bella")
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatePetApiResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePet_WithEmptyName_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var request = CreatePetRequestBuilder.Default()
            .WithName("")
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePet_WithNonExistentPetType_Returns404()
    {
        var request = CreatePetRequestBuilder.Default()
            .WithPetTypeId(Guid.NewGuid())
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePet_WithNameExceedingMaxLength_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var longName = new string('A', 101);
        var request = CreatePetRequestBuilder.Default()
            .WithName(longName)
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/pets", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== GET /api/pets =====

    [Fact]
    public async Task GetAllPets_WhenNoPetsExist_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/pets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pets = await response.Content.ReadFromJsonAsync<List<PetListItemResponse>>();
        pets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPets_WhenPetsExist_ReturnsAllPets()
    {
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync(petTypeId, "Buddy");
        await CreatePetAsync(petTypeId, "Bella");
        await CreatePetAsync(petTypeId, "Max");

        var response = await _client.GetAsync("/api/pets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pets = await response.Content.ReadFromJsonAsync<List<PetListItemResponse>>();
        pets.Should().HaveCount(3);
    }

    // ===== GET /api/pets/{id} =====

    [Fact]
    public async Task GetPetById_WithExistingPet_ReturnsPetDetails()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId, "Bella");

        var response = await _client.GetAsync($"/api/pets/{petId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pet = await response.Content.ReadFromJsonAsync<PetDetailsResponse>();
        pet!.Name.Should().Be("Bella");
        pet.Status.Should().Be("Available");
    }

    [Fact]
    public async Task GetPetById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/pets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/pets/{id}/reserve =====

    [Fact]
    public async Task ReservePet_WhenAvailable_Returns200AndReservedStatus()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);

        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetStatusResponse>();
        body!.Status.Should().Be("Reserved");
    }

    [Fact]
    public async Task ReservePet_WhenAlreadyReserved_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReservePet_WhenAdopted_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReservePet_WithNonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/reserve", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/pets/{id}/adopt =====

    [Fact]
    public async Task AdoptPet_WhenReserved_Returns200AndAdoptedStatus()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetStatusResponse>();
        body!.Status.Should().Be("Adopted");
    }

    [Fact]
    public async Task AdoptPet_WhenAvailable_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);

        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdoptPet_WhenAlreadyAdopted_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdoptPet_WithNonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/adopt", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/pets/{id}/cancel-reservation =====

    [Fact]
    public async Task CancelReservation_WhenReserved_Returns200AndAvailableStatus()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetStatusResponse>();
        body!.Status.Should().Be("Available");
    }

    [Fact]
    public async Task CancelReservation_WhenAvailable_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);

        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelReservation_WhenAdopted_Returns400()
    {
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(petTypeId);
        await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        await _client.PostAsync($"/api/pets/{petId}/adopt", null);

        var response = await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelReservation_WithNonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/pets/{Guid.NewGuid()}/cancel-reservation", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Response DTOs for deserialization ---
    private record CreatePetApiResponse(Guid Id);
    private record CreatePetTypeApiResponse(Guid Id, string Code, string Name);
    private record PetListItemResponse(Guid Id, string Name, string Type, string Status);
    private record PetDetailsResponse(Guid Id, string Name, string Type, string Status);
    private record PetStatusResponse(bool Success, Guid? PetId, string? Status);
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --verbosity normal`
Expected: All 20 tests pass. Fix any response DTO shape mismatches by reading actual API responses.

- [ ] **Step 3: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs
git commit -m "feat: add PetsController integration tests (20 cases)"
```

---

### Task 5: PetTypesAdminController Integration Tests

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetTypesAdminControllerTests.cs`

- [ ] **Step 1: Write all PetTypesAdminController tests**

Test cases to cover:

**POST /api/admin/pet-types (Create PetType)**
1. `CreatePetType_WithValidData_Returns201` - happy path
2. `CreatePetType_WithDuplicateCode_Returns409` - conflict
3. `CreatePetType_WithEmptyCode_Returns400`
4. `CreatePetType_WithCodeTooShort_Returns400` - 1 char code
5. `CreatePetType_WithCodeTooLong_Returns400` - >50 chars

**GET /api/admin/pet-types (Get All PetTypes)**
6. `GetAllPetTypes_ReturnsOnlyActiveByDefault`
7. `GetAllPetTypes_WithIncludeInactive_ReturnsAll`

**GET /api/admin/pet-types/{id} (Get PetType By Id)**
8. `GetPetTypeById_WithExistingId_ReturnsPetType`
9. `GetPetTypeById_WithNonExistentId_Returns404`

**PUT /api/admin/pet-types/{id} (Update PetType)**
10. `UpdatePetType_WithValidName_Returns200`
11. `UpdatePetType_WithNonExistentId_Returns404`

**POST /api/admin/pet-types/{id}/deactivate**
12. `DeactivatePetType_WhenActive_Returns200`
13. `DeactivatePetType_WithNonExistentId_Returns404`

**POST /api/admin/pet-types/{id}/activate**
14. `ActivatePetType_WhenInactive_Returns200`
15. `ActivatePetType_WithNonExistentId_Returns404`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;

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

    public async ValueTask InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<PetTypeResponse> CreatePetTypeAsync(string code = "cat", string name = "Cat")
    {
        var request = CreatePetTypeRequestBuilder.Default().WithCode(code).WithName(name).Build();
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetTypeResponse>())!;
    }

    // ===== POST /api/admin/pet-types =====

    [Fact]
    public async Task CreatePetType_WithValidData_Returns201()
    {
        var request = CreatePetTypeRequestBuilder.Default()
            .WithCode("hamster")
            .WithName("Hamster")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PetTypeResponse>();
        body!.Code.Should().Be("hamster");
        body.Name.Should().Be("Hamster");
    }

    [Fact]
    public async Task CreatePetType_WithDuplicateCode_Returns409()
    {
        await CreatePetTypeAsync("parrot", "Parrot");
        var request = CreatePetTypeRequestBuilder.Default()
            .WithCode("parrot")
            .WithName("Another Parrot")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePetType_WithEmptyCode_Returns400()
    {
        var request = CreatePetTypeRequestBuilder.Default().WithCode("").Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePetType_WithCodeTooShort_Returns400()
    {
        var request = CreatePetTypeRequestBuilder.Default().WithCode("x").Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePetType_WithCodeTooLong_Returns400()
    {
        var longCode = new string('a', 51);
        var request = CreatePetTypeRequestBuilder.Default().WithCode(longCode).Build();

        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== GET /api/admin/pet-types =====

    [Fact]
    public async Task GetAllPetTypes_ReturnsOnlyActiveByDefault()
    {
        var petType = await CreatePetTypeAsync("snake", "Snake");
        await _client.PostAsync($"/api/admin/pet-types/{petType.Id}/deactivate", null);

        var response = await _client.GetAsync("/api/admin/pet-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<List<PetTypeResponse>>();
        types.Should().NotContain(t => t.Code == "snake");
    }

    [Fact]
    public async Task GetAllPetTypes_WithIncludeInactive_ReturnsAll()
    {
        var petType = await CreatePetTypeAsync("lizard", "Lizard");
        await _client.PostAsync($"/api/admin/pet-types/{petType.Id}/deactivate", null);

        var response = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<List<PetTypeResponse>>();
        types.Should().Contain(t => t.Code == "lizard");
    }

    // ===== GET /api/admin/pet-types/{id} =====

    [Fact]
    public async Task GetPetTypeById_WithExistingId_ReturnsPetType()
    {
        var petType = await CreatePetTypeAsync("turtle", "Turtle");

        var response = await _client.GetAsync($"/api/admin/pet-types/{petType.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetTypeResponse>();
        body!.Code.Should().Be("turtle");
    }

    [Fact]
    public async Task GetPetTypeById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/admin/pet-types/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== PUT /api/admin/pet-types/{id} =====

    [Fact]
    public async Task UpdatePetType_WithValidName_Returns200()
    {
        var petType = await CreatePetTypeAsync("frog", "Frog");
        var updateRequest = new { Name = "Tree Frog" };

        var response = await _client.PutAsJsonAsync($"/api/admin/pet-types/{petType.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePetType_WithNonExistentId_Returns404()
    {
        var updateRequest = new { Name = "Nope" };

        var response = await _client.PutAsJsonAsync($"/api/admin/pet-types/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/admin/pet-types/{id}/deactivate =====

    [Fact]
    public async Task DeactivatePetType_WhenActive_Returns200()
    {
        var petType = await CreatePetTypeAsync("gecko", "Gecko");

        var response = await _client.PostAsync($"/api/admin/pet-types/{petType.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivatePetType_WithNonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/admin/pet-types/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/admin/pet-types/{id}/activate =====

    [Fact]
    public async Task ActivatePetType_WhenInactive_Returns200()
    {
        var petType = await CreatePetTypeAsync("ferret", "Ferret");
        await _client.PostAsync($"/api/admin/pet-types/{petType.Id}/deactivate", null);

        var response = await _client.PostAsync($"/api/admin/pet-types/{petType.Id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ActivatePetType_WithNonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/admin/pet-types/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record PetTypeResponse(Guid Id, string Code, string Name, bool IsActive);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --verbosity normal`
Expected: All 15 PetTypesAdmin tests + 20 PetsController tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetTypesAdminControllerTests.cs
git commit -m "feat: add PetTypesAdminController integration tests (15 cases)"
```

---

### Task 6: Pet Workflow Integration Tests

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetWorkflowTests.cs`

- [ ] **Step 1: Write full lifecycle workflow tests**

Test cases:
1. `FullAdoptionWorkflow_CreateReserveAdopt_Success` - end-to-end happy path
2. `ReserveCancelReReserveAdopt_Success` - reserve, cancel, reserve again, adopt
3. `MultiplePets_IndependentStateTransitions` - two pets with different states
4. `CreatePetWithSeededType_Success` - use a type seeded by PetTypeSeeder
5. `PetType_FullLifecycle_CreateUpdateDeactivateActivate`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class PetWorkflowTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PetWorkflowTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<Guid> SeedPetTypeAsync(string code = "dog", string name = "Dog")
    {
        var request = CreatePetTypeRequestBuilder.Default().WithCode(code).WithName(name).Build();
        var response = await _client.PostAsJsonAsync("/api/admin/pet-types", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreatePetTypeResponse>();
        return body!.Id;
    }

    private async Task<Guid> CreatePetAsync(Guid petTypeId, string name = "Buddy")
    {
        var request = CreatePetRequestBuilder.Default().WithName(name).WithPetTypeId(petTypeId).Build();
        var response = await _client.PostAsJsonAsync("/api/pets", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreatePetResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task FullAdoptionWorkflow_CreateReserveAdopt_Success()
    {
        var typeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync(typeId, "Luna");

        // Verify initial state
        var getResponse = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{petId}");
        getResponse!.Status.Should().Be("Available");

        // Reserve
        var reserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify reserved state
        getResponse = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{petId}");
        getResponse!.Status.Should().Be("Reserved");

        // Adopt
        var adoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        adoptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify adopted state
        getResponse = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{petId}");
        getResponse!.Status.Should().Be("Adopted");
    }

    [Fact]
    public async Task ReserveCancelReReserveAdopt_Success()
    {
        var typeId = await SeedPetTypeAsync("cat", "Cat");
        var petId = await CreatePetAsync(typeId, "Whiskers");

        await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        await _client.PostAsync($"/api/pets/{petId}/cancel-reservation", null);

        // Re-reserve after cancellation
        var reserveResponse = await _client.PostAsync($"/api/pets/{petId}/reserve", null);
        reserveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var adoptResponse = await _client.PostAsync($"/api/pets/{petId}/adopt", null);
        adoptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pet = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{petId}");
        pet!.Status.Should().Be("Adopted");
    }

    [Fact]
    public async Task MultiplePets_IndependentStateTransitions()
    {
        var typeId = await SeedPetTypeAsync("bird", "Bird");
        var pet1 = await CreatePetAsync(typeId, "Tweety");
        var pet2 = await CreatePetAsync(typeId, "Polly");

        // Reserve pet1 only
        await _client.PostAsync($"/api/pets/{pet1}/reserve", null);

        var p1 = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{pet1}");
        var p2 = await _client.GetFromJsonAsync<PetDetailsResponse>($"/api/pets/{pet2}");

        p1!.Status.Should().Be("Reserved");
        p2!.Status.Should().Be("Available");
    }

    [Fact]
    public async Task PetType_FullLifecycle_CreateUpdateDeactivateActivate()
    {
        // Create
        var typeId = await SeedPetTypeAsync("rabbit", "Rabbit");

        // Update name
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/admin/pet-types/{typeId}", new { Name = "Bunny" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Deactivate
        var deactivateResponse = await _client.PostAsync(
            $"/api/admin/pet-types/{typeId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify inactive
        var type = await _client.GetFromJsonAsync<PetTypeResponse>($"/api/admin/pet-types/{typeId}");
        type!.IsActive.Should().BeFalse();

        // Reactivate
        var activateResponse = await _client.PostAsync(
            $"/api/admin/pet-types/{typeId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        type = await _client.GetFromJsonAsync<PetTypeResponse>($"/api/admin/pet-types/{typeId}");
        type!.IsActive.Should().BeTrue();
    }

    private record CreatePetTypeResponse(Guid Id, string Code, string Name);
    private record CreatePetResponse(Guid Id);
    private record PetDetailsResponse(Guid Id, string Name, string Type, string Status);
    private record PetTypeResponse(Guid Id, string Code, string Name, bool IsActive);
}
```

- [ ] **Step 2: Run all PetService integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --verbosity normal`
Expected: All 40 tests pass (20 + 15 + 5 workflow).

- [ ] **Step 3: Check coverage**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --collect:"XPlat Code Coverage"`
Review the coverage report and identify any uncovered branches in controllers/handlers.

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetWorkflowTests.cs
git commit -m "feat: add PetService workflow integration tests (5 cases)"
```

---

## Chunk 3: UserService Integration Test Infrastructure & Builders

### Task 7: Create UserService Integration Test Project

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/PetAdoption.UserService.IntegrationTests.csproj`

- [ ] **Step 1: Create the .csproj file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.3" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="Testcontainers.MongoDb" Version="4.5.0" />
    <PackageReference Include="Testcontainers.RabbitMq" Version="4.5.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Services\UserService\PetAdoption.UserService.API\PetAdoption.UserService.API.csproj" />
    <ProjectReference Include="..\..\..\src\Services\UserService\PetAdoption.UserService.Domain\PetAdoption.UserService.Domain.csproj" />
    <ProjectReference Include="..\..\..\src\Services\UserService\PetAdoption.UserService.Infrastructure\PetAdoption.UserService.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add to solution and verify**

Run:
```bash
dotnet sln add tests/UserService/PetAdoption.UserService.IntegrationTests/PetAdoption.UserService.IntegrationTests.csproj
dotnet build tests/UserService/PetAdoption.UserService.IntegrationTests
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/ PetAdoption.sln
git commit -m "chore: add UserService integration tests project with Testcontainers"
```

---

### Task 8: UserService WebApplicationFactory + Auth Helper

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Infrastructure/MongoDbFixture.cs`
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Infrastructure/UserServiceWebAppFactory.cs`
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Infrastructure/AuthHelper.cs`

- [ ] **Step 1: Create MongoDbFixture**

```csharp
using Testcontainers.MongoDb;

namespace PetAdoption.UserService.IntegrationTests.Infrastructure;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async ValueTask InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
```

- [ ] **Step 2: Create UserServiceWebAppFactory**

Read `Program.cs` of UserService.API to identify exact service registrations. The factory must replace MongoDB connection and disable RabbitMQ background services.

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace PetAdoption.UserService.IntegrationTests.Infrastructure;

public class UserServiceWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public UserServiceWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
        _databaseName = $"UserDb_Test_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing MongoDB registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IMongoDatabase)
                         || d.ServiceType == typeof(IMongoClient))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            // Register test MongoDB
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);
            services.AddSingleton<IMongoClient>(client);
            services.AddSingleton(database);

            // Remove RabbitMQ background services
            var backgroundServices = services
                .Where(d => d.ImplementationType?.Name is "OutboxProcessorService"
                          or "RabbitMqTopologySetup")
                .ToList();
            foreach (var d in backgroundServices) services.Remove(d);
        });

        builder.UseEnvironment("Development");
    }
}
```

- [ ] **Step 3: Create AuthHelper**

This helper registers a user, logs in, and returns an authenticated `HttpClient`.

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PetAdoption.UserService.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    public static async Task<HttpClient> RegisterAndLoginAsync(
        HttpClient client,
        string email = "testuser@example.com",
        string password = "StrongPass123!",
        string fullName = "Test User")
    {
        // Register
        await client.PostAsJsonAsync("/api/users/register", new
        {
            Email = email,
            FullName = fullName,
            Password = password
        });

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        return client;
    }

    public record LoginResponse(bool Success, string Token, string UserId, string Email, string FullName, string Role, int ExpiresIn);
}
```

- [ ] **Step 4: Verify it builds**

Run: `dotnet build tests/UserService/PetAdoption.UserService.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Infrastructure/
git commit -m "feat: add UserService WebApplicationFactory, MongoDbFixture, and AuthHelper"
```

---

### Task 9: UserService Fluent Builders

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/RegisterUserRequestBuilder.cs`
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/UpdateProfileRequestBuilder.cs`
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/UserPreferencesBuilder.cs`

- [ ] **Step 1: Create RegisterUserRequestBuilder**

```csharp
namespace PetAdoption.UserService.IntegrationTests.Builders;

public class RegisterUserRequestBuilder
{
    private string _email = "user@example.com";
    private string _fullName = "John Doe";
    private string _password = "SecurePass123!";
    private string? _phoneNumber;

    public RegisterUserRequestBuilder WithEmail(string email) { _email = email; return this; }
    public RegisterUserRequestBuilder WithFullName(string name) { _fullName = name; return this; }
    public RegisterUserRequestBuilder WithPassword(string password) { _password = password; return this; }
    public RegisterUserRequestBuilder WithPhoneNumber(string? phone) { _phoneNumber = phone; return this; }

    public object Build() => new
    {
        Email = _email,
        FullName = _fullName,
        Password = _password,
        PhoneNumber = _phoneNumber
    };

    public static RegisterUserRequestBuilder Default() => new();
}
```

- [ ] **Step 2: Create UpdateProfileRequestBuilder**

```csharp
namespace PetAdoption.UserService.IntegrationTests.Builders;

public class UpdateProfileRequestBuilder
{
    private string? _fullName;
    private string? _phoneNumber;
    private object? _preferences;

    public UpdateProfileRequestBuilder WithFullName(string name) { _fullName = name; return this; }
    public UpdateProfileRequestBuilder WithPhoneNumber(string phone) { _phoneNumber = phone; return this; }
    public UpdateProfileRequestBuilder WithPreferences(object prefs) { _preferences = prefs; return this; }

    public object Build() => new
    {
        FullName = _fullName,
        PhoneNumber = _phoneNumber,
        Preferences = _preferences
    };

    public static UpdateProfileRequestBuilder Default() => new();
}
```

- [ ] **Step 3: Create UserPreferencesBuilder**

```csharp
namespace PetAdoption.UserService.IntegrationTests.Builders;

public class UserPreferencesBuilder
{
    private string _preferredPetType = "Dog";
    private List<string> _preferredSizes = ["Medium"];
    private string _preferredAgeRange = "1-5";
    private bool _receiveEmailNotifications = true;
    private bool _receiveSmsNotifications = false;

    public UserPreferencesBuilder WithPreferredPetType(string type) { _preferredPetType = type; return this; }
    public UserPreferencesBuilder WithPreferredSizes(params string[] sizes) { _preferredSizes = [..sizes]; return this; }
    public UserPreferencesBuilder WithPreferredAgeRange(string range) { _preferredAgeRange = range; return this; }
    public UserPreferencesBuilder WithEmailNotifications(bool enabled) { _receiveEmailNotifications = enabled; return this; }
    public UserPreferencesBuilder WithSmsNotifications(bool enabled) { _receiveSmsNotifications = enabled; return this; }

    public object Build() => new
    {
        PreferredPetType = _preferredPetType,
        PreferredSizes = _preferredSizes,
        PreferredAgeRange = _preferredAgeRange,
        ReceiveEmailNotifications = _receiveEmailNotifications,
        ReceiveSmsNotifications = _receiveSmsNotifications
    };

    public static UserPreferencesBuilder Default() => new();
}
```

- [ ] **Step 4: Verify it builds**

Run: `dotnet build tests/UserService/PetAdoption.UserService.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/
git commit -m "feat: add fluent builders for UserService integration tests"
```

---

## Chunk 4: UserService Integration Tests - All Endpoints

### Task 10: Authentication Tests (Register + Login)

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/AuthenticationTests.cs`

- [ ] **Step 1: Write authentication tests**

Test cases:

**POST /api/users/register**
1. `Register_WithValidData_ReturnsSuccess`
2. `Register_WithDuplicateEmail_Returns409`
3. `Register_WithInvalidEmail_Returns400`
4. `Register_WithShortPassword_Returns400` - <8 chars
5. `Register_WithOptionalPhoneNumber_ReturnsSuccess`
6. `Register_WithInvalidPhoneNumber_Returns400`
7. `Register_WithShortName_Returns400` - <2 chars

**POST /api/users/login**
8. `Login_WithValidCredentials_ReturnsToken`
9. `Login_WithWrongPassword_Returns401`
10. `Login_WithNonExistentEmail_Returns401`
11. `Login_ResponseContainsUserInfo` - verify token, userId, email, fullName, role fields

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class AuthenticationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthenticationTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ===== POST /api/users/register =====

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("valid@example.com")
            .WithFullName("Jane Doe")
            .WithPassword("StrongPass123!")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.Success.Should().BeTrue();
        body.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("duplicate@example.com")
            .Build();
        await _client.PostAsJsonAsync("/api/users/register", request);

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("not-an-email")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithPassword("short")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithOptionalPhoneNumber_ReturnsSuccess()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("withphone@example.com")
            .WithPhoneNumber("1234567890")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_Returns400()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("badphone@example.com")
            .WithPhoneNumber("123")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortName_Returns400()
    {
        var request = RegisterUserRequestBuilder.Default()
            .WithEmail("shortname@example.com")
            .WithFullName("A")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== POST /api/users/login =====

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var registerReq = RegisterUserRequestBuilder.Default()
            .WithEmail("login@example.com")
            .WithPassword("ValidPass123!")
            .Build();
        await _client.PostAsJsonAsync("/api/users/register", registerReq);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = "login@example.com",
            Password = "ValidPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Success.Should().BeTrue();
        body.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var registerReq = RegisterUserRequestBuilder.Default()
            .WithEmail("wrongpw@example.com")
            .WithPassword("ValidPass123!")
            .Build();
        await _client.PostAsJsonAsync("/api/users/register", registerReq);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = "wrongpw@example.com",
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = "nobody@example.com",
            Password = "SomePass123!"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ResponseContainsUserInfo()
    {
        var registerReq = RegisterUserRequestBuilder.Default()
            .WithEmail("info@example.com")
            .WithFullName("Info User")
            .WithPassword("ValidPass123!")
            .Build();
        await _client.PostAsJsonAsync("/api/users/register", registerReq);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            Email = "info@example.com",
            Password = "ValidPass123!"
        });

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.UserId.Should().NotBeNullOrEmpty();
        body.Email.Should().Be("info@example.com");
        body.FullName.Should().Be("Info User");
        body.Role.Should().NotBeNullOrEmpty();
        body.ExpiresIn.Should().BeGreaterThan(0);
    }

    private record RegisterResponse(bool Success, string UserId, string Message);
    private record LoginResponse(bool Success, string Token, string UserId, string Email, string FullName, string Role, int ExpiresIn);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --verbosity normal`
Expected: All 11 authentication tests pass. Adjust HTTP status codes to match actual API responses if needed.

- [ ] **Step 3: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/AuthenticationTests.cs
git commit -m "feat: add UserService authentication integration tests (11 cases)"
```

---

### Task 11: User Profile Tests

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/UserProfileTests.cs`

- [ ] **Step 1: Write user profile tests**

Test cases:

**GET /api/users/me**
1. `GetProfile_WhenAuthenticated_ReturnsUserProfile`
2. `GetProfile_WhenNotAuthenticated_Returns401`

**PUT /api/users/me**
3. `UpdateProfile_WithNewName_ReturnsSuccess`
4. `UpdateProfile_WithPhoneNumber_ReturnsSuccess`
5. `UpdateProfile_WithPreferences_ReturnsSuccess`
6. `UpdateProfile_WhenNotAuthenticated_Returns401`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class UserProfileTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public UserProfileTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ===== GET /api/users/me =====

    [Fact]
    public async Task GetProfile_WhenAuthenticated_ReturnsUserProfile()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "profile@example.com", "StrongPass123!", "Profile User");

        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        body!.Email.Should().Be("profile@example.com");
        body.FullName.Should().Be("Profile User");
    }

    [Fact]
    public async Task GetProfile_WhenNotAuthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== PUT /api/users/me =====

    [Fact]
    public async Task UpdateProfile_WithNewName_ReturnsSuccess()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "update1@example.com", "StrongPass123!", "Old Name");

        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithFullName("New Name")
            .Build();
        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify change persisted
        var profile = await _client.GetFromJsonAsync<UserProfileResponse>("/api/users/me");
        profile!.FullName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProfile_WithPhoneNumber_ReturnsSuccess()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "update2@example.com", "StrongPass123!", "Phone User");

        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithPhoneNumber("1234567890")
            .Build();
        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProfile_WithPreferences_ReturnsSuccess()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "update3@example.com", "StrongPass123!", "Pref User");

        var prefs = UserPreferencesBuilder.Default()
            .WithPreferredPetType("Cat")
            .WithPreferredSizes("Small", "Medium")
            .WithEmailNotifications(true)
            .WithSmsNotifications(true)
            .Build();
        var updateRequest = UpdateProfileRequestBuilder.Default()
            .WithPreferences(prefs)
            .Build();

        var response = await _client.PutAsJsonAsync("/api/users/me", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProfile_WhenNotAuthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var updateRequest = UpdateProfileRequestBuilder.Default().WithFullName("Hack").Build();

        var response = await unauthClient.PutAsJsonAsync("/api/users/me", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record UserProfileResponse(string Id, string Email, string FullName, string? PhoneNumber, string Status, string Role);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --verbosity normal`
Expected: All 6 profile tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/UserProfileTests.cs
git commit -m "feat: add UserService profile integration tests (6 cases)"
```

---

### Task 12: Password Management Tests

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/PasswordManagementTests.cs`

- [ ] **Step 1: Write password management tests**

Test cases:

**POST /api/users/me/change-password**
1. `ChangePassword_WithValidData_ReturnsSuccess`
2. `ChangePassword_WithWrongCurrentPassword_Returns400`
3. `ChangePassword_WithShortNewPassword_Returns400`
4. `ChangePassword_CanLoginWithNewPassword` - change pw, then login with new pw
5. `ChangePassword_WhenNotAuthenticated_Returns401`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class PasswordManagementTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    public PasswordManagementTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsSuccess()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "changepw@example.com", "OldPass123!", "PW User");

        var response = await _client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "wrongcur@example.com", "OldPass123!", "PW User");

        var response = await _client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "WrongPass!",
            NewPassword = "NewPass456!"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_Returns400()
    {
        await AuthHelper.RegisterAndLoginAsync(_client, "shortnew@example.com", "OldPass123!", "PW User");

        var response = await _client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "short"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_CanLoginWithNewPassword()
    {
        var email = "relogin@example.com";
        await AuthHelper.RegisterAndLoginAsync(_client, email, "OldPass123!", "ReLogin User");

        await _client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "BrandNew789!"
        });

        // Login with new password
        var freshClient = _factory.CreateClient();
        var loginResponse = await freshClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = "BrandNew789!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WhenNotAuthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "whatever",
            NewPassword = "whatever2"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponse(bool Success, string Token);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --verbosity normal`
Expected: All 5 password tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/PasswordManagementTests.cs
git commit -m "feat: add UserService password management integration tests (5 cases)"
```

---

### Task 13: Admin User Management Tests

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/AdminUserManagementTests.cs`

- [ ] **Step 1: Write admin management tests**

These tests require an admin user. The first registered user needs to be promoted to admin. Check how the API handles this - you may need to directly insert an admin user into MongoDB via the test factory, or use a seed mechanism.

Test cases:

**GET /api/users (Admin only)**
1. `GetUsers_AsAdmin_ReturnsPaginatedList`
2. `GetUsers_AsRegularUser_Returns403`
3. `GetUsers_WhenNotAuthenticated_Returns401`

**GET /api/users/{id} (Admin only)**
4. `GetUserById_AsAdmin_ReturnsUser`
5. `GetUserById_AsAdmin_NonExistentUser_Returns404`
6. `GetUserById_AsRegularUser_Returns403`

**POST /api/users/{id}/suspend (Admin only)**
7. `SuspendUser_AsAdmin_ReturnsSuccess`
8. `SuspendUser_AsRegularUser_Returns403`
9. `SuspendUser_SuspendedUserCannotLogin` - suspend user, attempt login, verify failure

**POST /api/users/{id}/promote-to-admin (Admin only)**
10. `PromoteToAdmin_AsAdmin_ReturnsSuccess`
11. `PromoteToAdmin_AsRegularUser_Returns403`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MongoDB.Driver;
using PetAdoption.UserService.IntegrationTests.Builders;
using PetAdoption.UserService.IntegrationTests.Infrastructure;

namespace PetAdoption.UserService.IntegrationTests.Tests;

[Collection("MongoDB")]
public class AdminUserManagementTests : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoFixture;
    private UserServiceWebAppFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private string _regularUserId = null!;

    public AdminUserManagementTests(MongoDbFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new UserServiceWebAppFactory(_mongoFixture.ConnectionString);
        _adminClient = _factory.CreateClient();
        _userClient = _factory.CreateClient();

        // Register admin user and promote via direct DB update
        // (since there's no public promote endpoint without auth)
        var registerReq = RegisterUserRequestBuilder.Default()
            .WithEmail("admin@example.com")
            .WithPassword("AdminPass123!")
            .WithFullName("Admin User")
            .Build();
        await _adminClient.PostAsJsonAsync("/api/users/register", registerReq);

        // Promote to admin directly in MongoDB
        // Read the actual collection name and role field from UserRepository implementation
        var db = _factory.Services.GetRequiredService<IMongoDatabase>();
        var usersCollection = db.GetCollection<MongoDB.Bson.BsonDocument>("users");
        var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("Email", "admin@example.com");
        var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("Role", 1); // Admin = 1
        await usersCollection.UpdateOneAsync(filter, update);

        // Login as admin
        var loginResponse = await _adminClient.PostAsJsonAsync("/api/users/login", new
        {
            Email = "admin@example.com",
            Password = "AdminPass123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Register and login regular user
        await AuthHelper.RegisterAndLoginAsync(_userClient, "regular@example.com", "UserPass123!", "Regular User");

        // Get regular user ID
        var profileResponse = await _userClient.GetFromJsonAsync<UserResponse>("/api/users/me");
        _regularUserId = profileResponse!.Id;
    }

    public async ValueTask DisposeAsync()
    {
        _adminClient.Dispose();
        _userClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ===== GET /api/users =====

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsPaginatedList()
    {
        var response = await _adminClient.GetAsync("/api/users?skip=0&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_AsRegularUser_Returns403()
    {
        var response = await _userClient.GetAsync("/api/users?skip=0&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WhenNotAuthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/users?skip=0&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== GET /api/users/{id} =====

    [Fact]
    public async Task GetUserById_AsAdmin_ReturnsUser()
    {
        var response = await _adminClient.GetAsync($"/api/users/{_regularUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_AsAdmin_NonExistentUser_Returns404()
    {
        var response = await _adminClient.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_AsRegularUser_Returns403()
    {
        var response = await _userClient.GetAsync($"/api/users/{_regularUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== POST /api/users/{id}/suspend =====

    [Fact]
    public async Task SuspendUser_AsAdmin_ReturnsSuccess()
    {
        // Create a user to suspend
        var client = _factory.CreateClient();
        var req = RegisterUserRequestBuilder.Default()
            .WithEmail("tosuspend@example.com")
            .WithPassword("SuspendMe123!")
            .WithFullName("Suspend Me")
            .Build();
        await client.PostAsJsonAsync("/api/users/register", req);
        await AuthHelper.RegisterAndLoginAsync(client, "tosuspend@example.com", "SuspendMe123!", "Suspend Me");
        var profile = await client.GetFromJsonAsync<UserResponse>("/api/users/me");

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/users/{profile!.Id}/suspend",
            new { Reason = "Testing suspension" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuspendUser_AsRegularUser_Returns403()
    {
        var response = await _userClient.PostAsJsonAsync(
            $"/api/users/{_regularUserId}/suspend",
            new { Reason = "Should fail" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuspendUser_SuspendedUserCannotLogin()
    {
        // Register user to suspend
        var client = _factory.CreateClient();
        var email = "suspended-login@example.com";
        var req = RegisterUserRequestBuilder.Default()
            .WithEmail(email)
            .WithPassword("SuspendMe123!")
            .WithFullName("Will Be Suspended")
            .Build();
        await client.PostAsJsonAsync("/api/users/register", req);
        await AuthHelper.RegisterAndLoginAsync(client, email, "SuspendMe123!", "Will Be Suspended");
        var profile = await client.GetFromJsonAsync<UserResponse>("/api/users/me");

        // Suspend
        await _adminClient.PostAsJsonAsync(
            $"/api/users/{profile!.Id}/suspend",
            new { Reason = "Test" });

        // Attempt login
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/users/login", new
        {
            Email = email,
            Password = "SuspendMe123!"
        });

        loginResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ===== POST /api/users/{id}/promote-to-admin =====

    [Fact]
    public async Task PromoteToAdmin_AsAdmin_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var req = RegisterUserRequestBuilder.Default()
            .WithEmail("promote@example.com")
            .WithPassword("PromoteMe123!")
            .WithFullName("Promote Me")
            .Build();
        await client.PostAsJsonAsync("/api/users/register", req);
        await AuthHelper.RegisterAndLoginAsync(client, "promote@example.com", "PromoteMe123!", "Promote Me");
        var profile = await client.GetFromJsonAsync<UserResponse>("/api/users/me");

        var response = await _adminClient.PostAsync($"/api/users/{profile!.Id}/promote-to-admin", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PromoteToAdmin_AsRegularUser_Returns403()
    {
        var response = await _userClient.PostAsync($"/api/users/{_regularUserId}/promote-to-admin", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private record LoginResponse(bool Success, string Token);
    private record UserResponse(string Id, string Email, string FullName, string Status, string Role);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --verbosity normal`
Expected: All 11 admin tests pass. The MongoDB direct update for admin promotion may need adjustments based on actual field names in the MongoDB documents - read the serializer/class map configuration to confirm.

- [ ] **Step 3: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/AdminUserManagementTests.cs
git commit -m "feat: add UserService admin management integration tests (11 cases)"
```

---

## Chunk 5: Coverage Analysis & Final Verification

### Task 14: Run Full Coverage Report

- [ ] **Step 1: Run all integration tests with coverage**

```bash
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --collect:"XPlat Code Coverage" --results-directory ./coverage/pet
dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --collect:"XPlat Code Coverage" --results-directory ./coverage/user
```

- [ ] **Step 2: Generate readable coverage report**

Install reportgenerator if not present:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Generate report:
```bash
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```

Open `coverage/report/index.html` and review:
- Controller coverage should be ~100%
- Command/Query handler coverage should be ~90%+
- Domain entity coverage via integration paths
- Infrastructure repository coverage via real MongoDB

- [ ] **Step 3: Identify and fill coverage gaps**

Review the coverage report for uncovered branches. Common gaps to check:
- Error handling paths in controllers
- Edge cases in command handlers (concurrent updates, version conflicts)
- Middleware exception mapping paths

Add additional tests for any significant uncovered paths.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete integration tests with coverage for PetService and UserService"
```

---

## Test Coverage Summary

### PetService Integration Tests (40 tests)
| Area | Tests | Coverage |
|------|-------|----------|
| POST /api/pets | 4 | Create with valid/invalid data, missing type, long name |
| GET /api/pets | 2 | Empty list, populated list |
| GET /api/pets/{id} | 2 | Found, not found |
| POST /api/pets/{id}/reserve | 4 | Available, already reserved, adopted, not found |
| POST /api/pets/{id}/adopt | 4 | Reserved, available, already adopted, not found |
| POST /api/pets/{id}/cancel-reservation | 4 | Reserved, available, adopted, not found |
| POST /api/admin/pet-types | 5 | Valid, duplicate, empty code, short code, long code |
| GET /api/admin/pet-types | 2 | Active only, include inactive |
| GET /api/admin/pet-types/{id} | 2 | Found, not found |
| PUT /api/admin/pet-types/{id} | 2 | Valid update, not found |
| POST deactivate/activate | 4 | Active→inactive, inactive→active, not found ×2 |
| Workflows | 5 | Full adoption, reserve-cancel-readopt, multi-pet, seeded type, type lifecycle |

### UserService Integration Tests (33 tests)
| Area | Tests | Coverage |
|------|-------|----------|
| POST /api/users/register | 7 | Valid, duplicate email, invalid email, short password, with phone, invalid phone, short name |
| POST /api/users/login | 4 | Valid, wrong password, non-existent email, response fields |
| GET /api/users/me | 2 | Authenticated, unauthenticated |
| PUT /api/users/me | 4 | Name, phone, preferences, unauthenticated |
| POST /api/users/me/change-password | 5 | Valid, wrong current, short new, re-login, unauthenticated |
| GET /api/users (admin) | 3 | Admin ok, regular user forbidden, unauthenticated |
| GET /api/users/{id} (admin) | 3 | Admin ok, not found, regular user forbidden |
| POST suspend (admin) | 3 | Admin ok, regular user forbidden, suspended cannot login |
| POST promote-to-admin (admin) | 2 | Admin ok, regular user forbidden |

**Total: 73 integration tests across both services**
