# PetService CRUD Endpoints Implementation Plan [COMPLETED]

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Update Pet, Delete Pet, and filtered/paginated Get All Pets endpoints to PetService, with full integration test coverage.

**Architecture:** Follow existing Clean Architecture (Domain → Application → Infrastructure → API). Each new endpoint gets a command/query + handler, following the exact patterns of existing handlers (e.g., `ReservePetCommandHandler`). The custom mediator auto-discovers handlers via reflection — no registration needed. New error codes added to `PetDomainErrorCode`, mapped in `ExceptionHandlingMiddleware`.

**Tech Stack:** .NET 9.0, MongoDB, xUnit, FluentAssertions, Testcontainers

---

## File Structure

### New files:
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs` — Update command, response, handler
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/DeletePetCommand.cs` — Delete command, response, handler
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs` — Filtered/paginated query, response DTO, handler
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/UpdatePetRequestBuilder.cs` — Builder for update requests

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs` — Add `UpdateName()` and `Delete()` domain methods
- `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` — Add `PetCannotBeDeleted` error code
- `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetRepository.cs` — Add `Delete(Guid id)` method
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs` — Add `GetFiltered()` method
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetRepository.cs` — Implement `Delete`
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs` — Implement `GetFiltered`
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` — Map new error code
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs` — Add PUT, DELETE, update GET endpoints
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/PetServiceWebAppFactory.cs` — Add `Delete` to `TestPetRepository`, add `GetFiltered` to `TestPetQueryStore`
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs` — Add integration tests for new endpoints

---

## Chunk 1: Domain + Application Layer

### Task 1: Add `UpdateName()` method to Pet aggregate

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`

- [ ] **Step 1: Add `UpdateName` method to Pet**

Add after the `CancelReservation()` method in `Pet.cs`:

```csharp
public void UpdateName(string newName)
{
    Name = new PetName(newName);
}
```

This delegates validation to `PetName` constructor (which throws `DomainException` with `InvalidPetName` for empty/too-long names). No need for extra validation.

- [ ] **Step 2: Run existing tests to verify no regressions**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`
Expected: All 67 tests pass

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs
git commit -m "add UpdateName method to Pet aggregate"
```

---

### Task 2: Add Delete support to Pet aggregate + error code

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs`

- [ ] **Step 1: Add `PetCannotBeDeleted` error code**

Add to `PetDomainErrorCode.cs` after `PetNotReserved`:

```csharp
/// <summary>
/// Pet cannot be deleted because it is reserved or adopted.
/// </summary>
public const string PetCannotBeDeleted = "pet_cannot_be_deleted";
```

- [ ] **Step 2: Add `EnsureCanBeDeleted()` method to Pet**

Add after `UpdateName()` in `Pet.cs`:

```csharp
public void EnsureCanBeDeleted()
{
    if (Status != PetStatus.Available)
    {
        throw new DomainException(
            PetDomainErrorCode.PetCannotBeDeleted,
            $"Pet {Id} cannot be deleted because it is {Status}. Only Available pets can be deleted.",
            new Dictionary<string, object>
            {
                { "PetId", Id },
                { "CurrentStatus", Status.ToString() }
            });
    }
}
```

- [ ] **Step 3: Run existing tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`
Expected: All 67 tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs
git commit -m "add delete validation to Pet aggregate and PetCannotBeDeleted error code"
```

---

### Task 3: Add `Delete` to IPetRepository + infrastructure

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetRepository.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetRepository.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`

- [ ] **Step 1: Add `Delete` to `IPetRepository`**

Add to `IPetRepository.cs`:

```csharp
Task Delete(Guid id);
```

- [ ] **Step 2: Implement `Delete` in `PetRepository`**

Add to `PetRepository.cs` after the `Update` method:

```csharp
public async Task Delete(Guid id)
{
    await _pets.DeleteOneAsync(p => p.Id == id);
}
```

- [ ] **Step 3: Map `PetCannotBeDeleted` in middleware**

In `ExceptionHandlingMiddleware.cs`, add to the `MapErrorCodeToHttpStatus` switch, in the "Business rule violations" section:

```csharp
PetDomainErrorCode.PetCannotBeDeleted => HttpStatusCode.Conflict,
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: Build succeeds (TestPetRepository will fail to compile — that's expected and fixed in Task 7)

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetRepository.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetRepository.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs
git commit -m "add Delete to IPetRepository and map PetCannotBeDeleted error code"
```

---

### Task 4: Add filtered/paginated query to IPetQueryStore + infrastructure

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`

- [ ] **Step 1: Add `GetFiltered` to `IPetQueryStore`**

Add to `IPetQueryStore.cs`:

```csharp
Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take);
```

Add using at top if not present:

```csharp
using PetAdoption.PetService.Domain;
```

- [ ] **Step 2: Implement `GetFiltered` in `PetQueryStore`**

Add to `PetQueryStore.cs`:

```csharp
public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take)
{
    var builder = Builders<Pet>.Filter;
    var filter = builder.Empty;

    if (status.HasValue)
        filter &= builder.Eq(p => p.Status, status.Value);

    if (petTypeId.HasValue)
        filter &= builder.Eq(p => p.PetTypeId, petTypeId.Value);

    var total = await _pets.CountDocumentsAsync(filter);
    var pets = await _pets.Find(filter)
        .Skip(skip)
        .Limit(take)
        .ToListAsync();

    return (pets, total);
}
```

Add using at top of `PetQueryStore.cs`:

```csharp
using PetAdoption.PetService.Domain;
```

Note: `Builders<Pet>` requires `using MongoDB.Driver;` which is already present.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs
git commit -m "add filtered paginated query to IPetQueryStore"
```

---

### Task 5: Create UpdatePetCommand handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs`

- [ ] **Step 1: Create UpdatePetCommand with handler**

Create `UpdatePetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdatePetCommand(Guid PetId, string Name) : IRequest<UpdatePetResponse>;

public record UpdatePetResponse(Guid Id, string Name, string Status);

public class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand, UpdatePetResponse>
{
    private readonly IPetRepository _repository;

    public UpdatePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdatePetResponse> Handle(UpdatePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        pet.UpdateName(request.Name);
        await _repository.Update(pet);

        return new UpdatePetResponse(pet.Id, pet.Name, pet.Status.ToString());
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs
git commit -m "add UpdatePetCommand handler"
```

---

### Task 6: Create DeletePetCommand handler and GetPetsQuery handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/DeletePetCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs`

- [ ] **Step 1: Create DeletePetCommand with handler**

Create `DeletePetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeletePetCommand(Guid PetId) : IRequest<DeletePetResponse>;

public record DeletePetResponse(bool Success, string Message);

public class DeletePetCommandHandler : IRequestHandler<DeletePetCommand, DeletePetResponse>
{
    private readonly IPetRepository _repository;

    public DeletePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeletePetResponse> Handle(DeletePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        pet.EnsureCanBeDeleted();
        await _repository.Delete(pet.Id);

        return new DeletePetResponse(true, $"Pet '{pet.Name}' has been deleted.");
    }
}
```

- [ ] **Step 2: Create GetPetsQuery with handler**

Create `GetPetsQuery.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetsQuery(
    PetStatus? Status,
    Guid? PetTypeId,
    int Skip = 0,
    int Take = 20) : IRequest<GetPetsResponse>;

public record GetPetsResponse(
    List<PetListItemDto> Pets,
    long Total,
    int Skip,
    int Take);

public class GetPetsQueryHandler : IRequestHandler<GetPetsQuery, GetPetsResponse>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetPetsQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetPetsResponse> Handle(GetPetsQuery request, CancellationToken cancellationToken = default)
    {
        var (pets, total) = await _queryStore.GetFiltered(
            request.Status,
            request.PetTypeId,
            request.Skip,
            request.Take);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = pets.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString()
        )).ToList();

        return new GetPetsResponse(items, total, request.Skip, request.Take);
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/DeletePetCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs
git commit -m "add DeletePetCommand and GetPetsQuery handlers"
```

---

## Chunk 2: API Layer + Test Infrastructure

### Task 7: Update PetsController with new endpoints

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`

- [ ] **Step 1: Add UpdatePetRequest record**

Add at the bottom of `PetsController.cs` (next to `CreatePetRequest`):

```csharp
public record UpdatePetRequest(string Name);
```

- [ ] **Step 2: Add PUT endpoint for updating a pet**

Add after the `GetById` action in `PetsController.cs`:

```csharp
// PUT /api/pets/{id}
[HttpPut("{id}")]
public async Task<ActionResult<UpdatePetResponse>> Update(Guid id, UpdatePetRequest request)
{
    var result = await _mediator.Send(new UpdatePetCommand(id, request.Name));
    return Ok(result);
}
```

Add `using PetAdoption.PetService.Application.Commands;` if `UpdatePetCommand` is not already resolved (it should be — `CreatePetCommand` is in the same namespace).

- [ ] **Step 3: Add DELETE endpoint**

Add after the PUT endpoint:

```csharp
// DELETE /api/pets/{id}
[HttpDelete("{id}")]
public async Task<ActionResult<DeletePetResponse>> Delete(Guid id)
{
    var result = await _mediator.Send(new DeletePetCommand(id));
    return Ok(result);
}
```

- [ ] **Step 4: Add filtered GET endpoint**

Replace the existing `GetAll` action with a new version that supports filtering and pagination, while keeping backward compatibility (all params are optional):

```csharp
// GET /api/pets?status=Available&petTypeId=...&skip=0&take=20
[HttpGet]
public async Task<ActionResult<GetPetsResponse>> GetAll(
    [FromQuery] string? status = null,
    [FromQuery] Guid? petTypeId = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20)
{
    PetStatus? petStatus = null;
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<PetStatus>(status, true, out var parsed))
    {
        petStatus = parsed;
    }

    var result = await _mediator.Send(new GetPetsQuery(petStatus, petTypeId, skip, take));
    return Ok(result);
}
```

Remove the old `using PetAdoption.PetService.Application.DTOs;` import only if no longer needed (it is — `PetDetailsDto` is still used by `GetById`). Add:

```csharp
using PetAdoption.PetService.Domain;
```

- [ ] **Step 5: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: Build succeeds

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs
git commit -m "add PUT, DELETE, and filtered GET endpoints to PetsController"
```

---

### Task 8: Update test infrastructure

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/PetServiceWebAppFactory.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/UpdatePetRequestBuilder.cs`

- [ ] **Step 1: Add `Delete` to `TestPetRepository`**

Add to the `TestPetRepository` class in `PetServiceWebAppFactory.cs`:

```csharp
public async Task Delete(Guid id)
{
    await _pets.DeleteOneAsync(p => p.Id == id);
}
```

- [ ] **Step 2: Add `GetFiltered` to `TestPetQueryStore`**

Add to the `TestPetQueryStore` class in `PetServiceWebAppFactory.cs`:

```csharp
public async Task<(IEnumerable<PetAdoption.PetService.Domain.Pet> Pets, long Total)> GetFiltered(
    PetAdoption.PetService.Domain.PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take)
{
    var builder = Builders<PetAdoption.PetService.Domain.Pet>.Filter;
    var filter = builder.Empty;

    if (status.HasValue)
        filter &= builder.Eq(p => p.Status, status.Value);

    if (petTypeId.HasValue)
        filter &= builder.Eq(p => p.PetTypeId, petTypeId.Value);

    var total = await _pets.CountDocumentsAsync(filter);
    var pets = await _pets.Find(filter)
        .Skip(skip)
        .Limit(take)
        .ToListAsync();

    return (pets, total);
}
```

- [ ] **Step 3: Create `UpdatePetRequestBuilder`**

Create `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/UpdatePetRequestBuilder.cs`:

```csharp
using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class UpdatePetRequestBuilder
{
    private string _name = "UpdatedName";

    public UpdatePetRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdatePetRequest Build() => new(_name);

    public static UpdatePetRequestBuilder Default() => new();
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Infrastructure/PetServiceWebAppFactory.cs tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/UpdatePetRequestBuilder.cs
git commit -m "update test infrastructure for new Pet endpoints"
```

---

## Chunk 3: Integration Tests

### Task 9: Integration tests for PUT /api/pets/{id} (Update Pet)

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`

- [ ] **Step 1: Add response DTO for update**

Add to the response DTOs section at the bottom of `PetsControllerTests.cs`:

```csharp
private record UpdatePetResponseDto(Guid Id, string Name, string Status);

private record DeletePetResponseDto(bool Success, string Message);

private record GetPetsResponseDto(List<PetListItemResponseDto> Pets, long Total, int Skip, int Take);
```

- [ ] **Step 2: Add Update Pet integration tests**

Add after the `// POST /api/pets (Create Pet)` section (or at the end before DTOs section). Add a new section:

```csharp
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
```

- [ ] **Step 3: Add using for UpdatePetRequestBuilder**

Add at top of `PetsControllerTests.cs` if not already there (it should be covered by `using PetAdoption.PetService.IntegrationTests.Builders;`).

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~UpdatePet"`
Expected: All 5 tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs
git commit -m "add integration tests for PUT /api/pets/{id}"
```

---

### Task 10: Integration tests for DELETE /api/pets/{id}

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`

- [ ] **Step 1: Add Delete Pet integration tests**

Add a new section:

```csharp
// ──────────────────────────────────────────────────────────────
// DELETE /api/pets/{id} (Delete Pet)
// ──────────────────────────────────────────────────────────────

[Fact]
public async Task DeletePet_WhenAvailable_ReturnsOkAndSuccess()
{
    // Arrange
    var petTypeId = await SeedPetTypeAsync();
    var petId = await CreatePetAsync("Buddy", petTypeId);

    // Act
    var response = await _client.DeleteAsync($"/api/pets/{petId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<DeletePetResponseDto>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
    result.Message.Should().Contain("Buddy");
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
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Act
    var response = await _client.GetAsync($"/api/pets/{petId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~DeletePet"`
Expected: All 5 tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs
git commit -m "add integration tests for DELETE /api/pets/{id}"
```

---

### Task 11: Integration tests for filtered/paginated GET /api/pets

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`

- [ ] **Step 1: Update existing GetAll tests**

The existing `GetAllPets_WhenNoPetsExist_ReturnsEmptyList` and `GetAllPets_WhenPetsExist_ReturnsAllPets` tests use the old response format (`List<PetListItemResponseDto>`). Update them to use the new paginated response.

Replace `GetAllPets_WhenNoPetsExist_ReturnsEmptyList`:

```csharp
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
```

Replace `GetAllPets_WhenPetsExist_ReturnsAllPets`:

```csharp
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
```

- [ ] **Step 2: Add filter and pagination tests**

Add a new section:

```csharp
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
    var dog1 = await CreatePetAsync("Buddy", dogTypeId);     // Available dog
    await CreateAndReservePetAsync(dogTypeId);                 // Reserved dog
    await CreatePetAsync("Whiskers", catTypeId);              // Available cat

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
```

- [ ] **Step 3: Run all integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: All tests pass (existing 20 + 5 update + 5 delete + 5 filtered + 2 updated existing = ~35 total, some existing tests are also updated)

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs
git commit -m "add integration tests for filtered/paginated GET /api/pets"
```

---

### Task 12: Update PetService CLAUDE.md

**Files:**
- Modify: `src/Services/PetService/CLAUDE.md`

- [ ] **Step 1: Update the API Endpoints section**

Update the Pets table in `CLAUDE.md` to include new endpoints:

```markdown
### Pets (`/api/pets`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/pets` | List pets (filtered, paginated: `?status=&petTypeId=&skip=&take=`) |
| POST | `/api/pets` | Create pet |
| GET | `/api/pets/{id}` | Get pet by ID |
| PUT | `/api/pets/{id}` | Update pet name |
| DELETE | `/api/pets/{id}` | Delete pet (only Available) |
| POST | `/api/pets/{id}/reserve` | Reserve pet |
| POST | `/api/pets/{id}/adopt` | Adopt pet |
| POST | `/api/pets/{id}/cancel-reservation` | Cancel reservation |
```

- [ ] **Step 2: Update the Domain Model section**

Add to the Pet Aggregate section:

```markdown
- Methods: `Reserve()`, `Adopt()`, `CancelReservation()`, `UpdateName()`, `EnsureCanBeDeleted()`
```

- [ ] **Step 3: Update error code table**

Add to the error code table:

```markdown
| `pet_cannot_be_deleted` | 409 |
```

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/CLAUDE.md
git commit -m "update PetService CLAUDE.md with new endpoints"
```

---

### Task 13: Run full test suite

- [ ] **Step 1: Run all PetService unit tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`
Expected: All tests pass

- [ ] **Step 2: Run all PetService integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`
Expected: All tests pass

- [ ] **Step 3: Verify no regressions across the full solution**

Run: `dotnet test PetAdoption.sln`
Expected: All tests pass
