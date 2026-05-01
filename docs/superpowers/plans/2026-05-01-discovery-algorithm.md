# Pet Discovery Algorithm Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Tinder-like discovery algorithm that shows users only pets they haven't seen, respects their filters, and gracefully handles the "no more pets" state.

**Architecture:** New PetSkip entity tracks skipped pets. A dedicated `/api/discover` endpoint queries Available pets while excluding the user's favorites and skips via SQL LEFT JOIN / NOT IN. The Discover page switches from the generic pets endpoint to this personalized feed. Skip tracking enables the exclusion, and a reset endpoint lets users re-discover.

**Tech Stack:** .NET 9.0, EF Core + SQL Server, custom mediator, Blazor WASM + MudBlazor 8.x

**Dependencies:** Plan 8 (Discover Pet Filters) should be completed first for filter support, but this plan can work independently by adding its own filter parameters.

---

## File Structure

### New files:
- `src/Services/PetService/PetAdoption.PetService.Domain/PetSkip.cs` -- PetSkip entity
- `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetSkipRepository.cs` -- Write repository interface
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackSkipCommand.cs` -- Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackSkipCommandHandler.cs` -- Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/ResetSkipsCommand.cs` -- Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/ResetSkipsCommandHandler.cs` -- Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetDiscoverPetsQuery.cs` -- Query, response, handler (core algorithm)
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetSkipRepository.cs` -- EF Core implementation
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/SkipsController.cs` -- POST/DELETE skip endpoints
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/DiscoverController.cs` -- GET /api/discover endpoint
- `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetSkipTests.cs` -- Unit tests for PetSkip entity
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/DiscoverControllerTests.cs` -- Integration tests for discovery algorithm

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` -- Add `SkipAlreadyExists` error code
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` -- Add `PetSkips` DbSet + entity config
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` -- Map new error code
- `src/Services/PetService/PetAdoption.PetService.API/Program.cs` -- Register `IPetSkipRepository`
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` -- Add `GetDiscoverPetsAsync`, `TrackSkipAsync`, `ResetSkipsAsync`
- `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` -- Add `DiscoverPetsResponse` record
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor` -- Switch to discovery endpoint, track skips, empty state

---

## Chunk 1: Domain -- PetSkip Entity + Repository Interface + Unit Tests

### Task 1.1: Create PetSkip domain entity

**File:** `src/Services/PetService/PetAdoption.PetService.Domain/PetSkip.cs` (new)

- [ ] **Step 1: Create PetSkip entity**

```csharp
namespace PetAdoption.PetService.Domain;

public class PetSkip
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PetId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PetSkip() { }

    public static PetSkip Create(Guid userId, Guid petId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId cannot be empty.", nameof(petId));

        return new PetSkip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PetId = petId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

### Task 1.2: Create IPetSkipRepository interface

**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetSkipRepository.cs` (new)

- [ ] **Step 2: Create IPetSkipRepository**

```csharp
namespace PetAdoption.PetService.Domain.Interfaces;

public interface IPetSkipRepository
{
    Task AddAsync(PetSkip skip);
    Task<PetSkip?> GetByUserAndPetAsync(Guid userId, Guid petId);
    Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId);
    Task DeleteAllByUserAsync(Guid userId);
}
```

### Task 1.3: Add SkipAlreadyExists error code

**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` (modify)

- [ ] **Step 3: Add error code for duplicate skip**

Add after the `FavoriteNotFound` constant:

```csharp
// Skip errors

/// <summary>
/// Pet is already in the user's skips.
/// </summary>
public const string SkipAlreadyExists = "skip_already_exists";
```

### Task 1.4: Write PetSkip unit tests

**File:** `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetSkipTests.cs` (new)

- [ ] **Step 4: Create PetSkip unit tests**

```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain;

public class PetSkipTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        // Act
        var skip = PetSkip.Create(userId, petId);

        // Assert
        skip.Id.Should().NotBeEmpty();
        skip.UserId.Should().Be(userId);
        skip.PetId.Should().Be(petId);
        skip.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetSkip.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetSkip.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 5: Run unit tests to verify**

```bash
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
```

Expected: All existing tests pass + 3 new PetSkip tests pass.

- [ ] **Step 6: Commit**

```
add PetSkip domain entity, repository interface, and unit tests
```

---

## Chunk 2: Infrastructure -- EF Core Mapping, PetSkipRepository, DI Registration

### Task 2.1: Add PetSkips to DbContext

**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` (modify)

- [ ] **Step 1: Add PetSkips DbSet**

Add after `public DbSet<Favorite> Favorites => Set<Favorite>();`:

```csharp
public DbSet<PetSkip> PetSkips => Set<PetSkip>();
```

- [ ] **Step 2: Add PetSkip entity configuration**

Add the following inside `OnModelCreating`, after the `Favorite` entity configuration block:

```csharp
modelBuilder.Entity<PetSkip>(entity =>
{
    entity.ToTable("PetSkips");
    entity.HasKey(s => s.Id);
    entity.HasIndex(s => new { s.UserId, s.PetId }).IsUnique();
});
```

### Task 2.2: Implement PetSkipRepository

**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetSkipRepository.cs` (new)

- [ ] **Step 3: Create PetSkipRepository**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetSkipRepository : IPetSkipRepository
{
    private readonly PetServiceDbContext _db;

    public PetSkipRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PetSkip skip)
    {
        _db.PetSkips.Add(skip);
        await _db.SaveChangesAsync();
    }

    public async Task<PetSkip?> GetByUserAndPetAsync(Guid userId, Guid petId)
    {
        return await _db.PetSkips
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PetId == petId);
    }

    public async Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId)
    {
        return await _db.PetSkips
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.PetId)
            .ToListAsync();
    }

    public async Task DeleteAllByUserAsync(Guid userId)
    {
        await _db.PetSkips
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
```

### Task 2.3: Register IPetSkipRepository in DI

**File:** `src/Services/PetService/PetAdoption.PetService.API/Program.cs` (modify)

- [ ] **Step 4: Add repository registration**

Add after `builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();`:

```csharp
builder.Services.AddScoped<IPetSkipRepository, PetSkipRepository>();
```

Also add the using directive at the top if not already covered by global usings. The existing `using PetAdoption.PetService.Domain.Interfaces;` already covers the interface. The `PetSkipRepository` lives in `PetAdoption.PetService.Infrastructure.Persistence` which is already imported.

### Task 2.4: Map SkipAlreadyExists in middleware

**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` (modify)

- [ ] **Step 5: Add error code mapping**

Add inside `MapErrorCodeToHttpStatus`, in the "Business rule violations" section, after `PetDomainErrorCode.FavoriteAlreadyExists`:

```csharp
PetDomainErrorCode.SkipAlreadyExists => HttpStatusCode.Conflict,
```

- [ ] **Step 6: Build to verify compilation**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.API
```

- [ ] **Step 7: Commit**

```
add PetSkip EF Core mapping, repository implementation, and DI registration
```

---

## Chunk 3: Application -- TrackSkipCommand, ResetSkipsCommand, GetDiscoverPetsQuery

### Task 3.1: Create TrackSkipCommand

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackSkipCommand.cs` (new)

- [ ] **Step 1: Create TrackSkipCommand record**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record TrackSkipCommand(Guid UserId, Guid PetId) : IRequest<TrackSkipResponse>;
```

### Task 3.2: Create TrackSkipCommandHandler

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackSkipCommandHandler.cs` (new)

- [ ] **Step 2: Create TrackSkipCommandHandler**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record TrackSkipResponse(Guid Id, Guid PetId, DateTime CreatedAt);

public class TrackSkipCommandHandler : IRequestHandler<TrackSkipCommand, TrackSkipResponse>
{
    private readonly IPetSkipRepository _skipRepository;
    private readonly IPetRepository _petRepository;

    public TrackSkipCommandHandler(IPetSkipRepository skipRepository, IPetRepository petRepository)
    {
        _skipRepository = skipRepository;
        _petRepository = petRepository;
    }

    public async Task<TrackSkipResponse> Handle(TrackSkipCommand request, CancellationToken ct)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");

        var existing = await _skipRepository.GetByUserAndPetAsync(request.UserId, request.PetId);
        if (existing is not null)
            throw new DomainException(PetDomainErrorCode.SkipAlreadyExists, "Pet is already skipped.");

        var skip = PetSkip.Create(request.UserId, request.PetId);
        await _skipRepository.AddAsync(skip);

        return new TrackSkipResponse(skip.Id, skip.PetId, skip.CreatedAt);
    }
}
```

### Task 3.3: Create ResetSkipsCommand

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/ResetSkipsCommand.cs` (new)

- [ ] **Step 3: Create ResetSkipsCommand record**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record ResetSkipsCommand(Guid UserId) : IRequest<ResetSkipsResponse>;
```

### Task 3.4: Create ResetSkipsCommandHandler

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/ResetSkipsCommandHandler.cs` (new)

- [ ] **Step 4: Create ResetSkipsCommandHandler**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

public record ResetSkipsResponse(bool Success);

public class ResetSkipsCommandHandler : IRequestHandler<ResetSkipsCommand, ResetSkipsResponse>
{
    private readonly IPetSkipRepository _skipRepository;

    public ResetSkipsCommandHandler(IPetSkipRepository skipRepository)
    {
        _skipRepository = skipRepository;
    }

    public async Task<ResetSkipsResponse> Handle(ResetSkipsCommand request, CancellationToken ct)
    {
        await _skipRepository.DeleteAllByUserAsync(request.UserId);
        return new ResetSkipsResponse(true);
    }
}
```

### Task 3.5: Create GetDiscoverPetsQuery (the core algorithm)

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetDiscoverPetsQuery.cs` (new)

- [ ] **Step 5: Create GetDiscoverPetsQuery, response, and handler**

This is the core discovery algorithm. It:
1. Gets all pet IDs the user has favorited
2. Gets all pet IDs the user has skipped
3. Queries Available pets excluding both sets
4. Applies optional filters (petTypeId, age range)
5. Returns the batch with no offset pagination (exclusion-based)

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetDiscoverPetsQuery(
    Guid UserId,
    Guid? PetTypeId,
    int? MinAgeMonths,
    int? MaxAgeMonths,
    int Take = 10) : IRequest<GetDiscoverPetsResponse>;

public record GetDiscoverPetsResponse(
    List<PetListItemDto> Pets,
    bool HasMore);

public class GetDiscoverPetsQueryHandler : IRequestHandler<GetDiscoverPetsQuery, GetDiscoverPetsResponse>
{
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetSkipRepository _skipRepository;
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetDiscoverPetsQueryHandler(
        IPetQueryStore petQueryStore,
        IPetSkipRepository skipRepository,
        IFavoriteRepository favoriteRepository,
        IPetTypeRepository petTypeRepository)
    {
        _petQueryStore = petQueryStore;
        _skipRepository = skipRepository;
        _favoriteRepository = favoriteRepository;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetDiscoverPetsResponse> Handle(GetDiscoverPetsQuery request, CancellationToken ct)
    {
        // 1. Get user's skipped pet IDs
        var skippedPetIds = await _skipRepository.GetPetIdsByUserAsync(request.UserId);

        // 2. Get user's favorited pet IDs
        var favoritedPetIds = await _favoriteRepository.GetPetIdsByUserAsync(request.UserId);

        // 3. Combine exclusion set
        var excludedIds = skippedPetIds.Concat(favoritedPetIds).ToHashSet();

        // 4. Query via query store (delegating filtering to infrastructure)
        var (pets, _) = await _petQueryStore.GetDiscoverable(
            excludedIds,
            request.PetTypeId,
            request.MinAgeMonths,
            request.MaxAgeMonths,
            request.Take + 1); // fetch one extra to determine HasMore

        var petList = pets.ToList();
        var hasMore = petList.Count > request.Take;
        if (hasMore)
            petList = petList.Take(request.Take).ToList();

        // 5. Map to DTOs
        var petTypes = await _petTypeRepository.GetAllAsync(ct);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = petList.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString(),
            p.Breed?.Value,
            p.Age?.Months,
            p.Description?.Value
        )).ToList();

        return new GetDiscoverPetsResponse(items, hasMore);
    }
}
```

### Task 3.6: Add GetDiscoverable and GetPetIdsByUserAsync to query/repository interfaces

**File:** `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs` (modify)

- [ ] **Step 6: Add GetDiscoverable method to IPetQueryStore**

Add to the interface:

```csharp
Task<(IEnumerable<Pet> Pets, long Total)> GetDiscoverable(
    HashSet<Guid> excludedPetIds,
    Guid? petTypeId,
    int? minAgeMonths,
    int? maxAgeMonths,
    int take);
```

**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IFavoriteRepository.cs` (modify)

- [ ] **Step 7: Add GetPetIdsByUserAsync to IFavoriteRepository**

Add to the interface:

```csharp
Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId);
```

### Task 3.7: Implement GetDiscoverable in PetQueryStore

**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs` (modify)

- [ ] **Step 8: Add GetDiscoverable implementation**

Add the following method to `PetQueryStore`:

```csharp
public async Task<(IEnumerable<Pet> Pets, long Total)> GetDiscoverable(
    HashSet<Guid> excludedPetIds,
    Guid? petTypeId,
    int? minAgeMonths,
    int? maxAgeMonths,
    int take)
{
    var query = _db.Pets.AsNoTracking()
        .Where(p => p.Status == PetStatus.Available);

    if (excludedPetIds.Count > 0)
        query = query.Where(p => !excludedPetIds.Contains(p.Id));

    if (petTypeId.HasValue)
        query = query.Where(p => p.PetTypeId == petTypeId.Value);

    if (minAgeMonths.HasValue)
        query = query.Where(p => p.Age != null && p.Age.Months >= minAgeMonths.Value);

    if (maxAgeMonths.HasValue)
        query = query.Where(p => p.Age != null && p.Age.Months <= maxAgeMonths.Value);

    var total = await query.LongCountAsync();
    var pets = await query.Take(take).ToListAsync();

    return (pets, total);
}
```

### Task 3.8: Implement GetPetIdsByUserAsync in FavoriteRepository

**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteRepository.cs` (modify)

- [ ] **Step 9: Add GetPetIdsByUserAsync implementation**

Add the following method to `FavoriteRepository`:

```csharp
public async Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId)
{
    return await _db.Favorites
        .AsNoTracking()
        .Where(f => f.UserId == userId)
        .Select(f => f.PetId)
        .ToListAsync();
}
```

- [ ] **Step 10: Build to verify compilation**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.API
```

- [ ] **Step 11: Commit**

```
add TrackSkip, ResetSkips commands and GetDiscoverPets query with core algorithm
```

---

## Chunk 4: API -- SkipsController + DiscoverController

### Task 4.1: Create SkipsController

**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/SkipsController.cs` (new)

- [ ] **Step 1: Create SkipsController**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/skips")]
[Authorize]
public class SkipsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkipsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Track a pet skip (user swiped left / dismissed the pet).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TrackSkip([FromBody] TrackSkipRequest request)
    {
        var result = await _mediator.Send(new TrackSkipCommand(GetUserId(), request.PetId));
        return StatusCode(201, result);
    }

    /// <summary>
    /// Reset all skips for the current user, allowing them to re-discover all pets.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ResetSkips()
    {
        await _mediator.Send(new ResetSkipsCommand(GetUserId()));
        return NoContent();
    }
}

public record TrackSkipRequest(Guid PetId);
```

### Task 4.2: Create DiscoverController

**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/DiscoverController.cs` (new)

- [ ] **Step 2: Create DiscoverController**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/discover")]
[Authorize]
public class DiscoverController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscoverController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Get personalized pet discovery feed. Returns Available pets the user
    /// hasn't liked or skipped yet, filtered by optional criteria.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Discover(
        [FromQuery] Guid? petTypeId = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] int take = 10)
    {
        var result = await _mediator.Send(new GetDiscoverPetsQuery(
            GetUserId(),
            petTypeId,
            minAge,
            maxAge,
            take));

        return Ok(result);
    }
}
```

- [ ] **Step 3: Build to verify compilation**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.API
```

- [ ] **Step 4: Commit**

```
add SkipsController and DiscoverController API endpoints
```

---

## Chunk 5: Integration Tests -- Discovery Algorithm

### Task 5.1: Create DiscoverControllerTests

**File:** `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/DiscoverControllerTests.cs` (new)

- [ ] **Step 1: Create integration tests for the discovery algorithm**

These tests verify the core behaviors:
- Discovery returns Available pets
- Discovery excludes favorited pets
- Discovery excludes skipped pets
- Discovery returns empty when all pets are seen
- Discovery respects petTypeId filter
- Discovery respects age filters
- Skip tracking works (POST /api/skips)
- Skip reset works (DELETE /api/skips)
- Unauthenticated requests return 401

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class DiscoverControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public DiscoverControllerTests(SqlServerFixture sqlFixture)
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

        var allTypesResponse = await _client.GetAsync("/api/admin/pet-types?includeInactive=true");
        allTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        var existing = allTypes!.First(t => t.Code == code);
        return existing.Id;
    }

    private async Task<Guid> CreatePetAsync(string name = "Buddy", Guid? petTypeId = null, int? ageMonths = null)
    {
        var typeId = petTypeId ?? await SeedPetTypeAsync();

        var request = new CreatePetRequestBuilder()
            .WithName(name)
            .WithPetTypeId(typeId);

        if (ageMonths.HasValue)
            request = request.WithAgeMonths(ageMonths.Value);

        var response = await _client.PostAsJsonAsync("/api/pets", request.Build());
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
        result.Should().NotBeNull();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/discover (Discovery Feed)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_WithAvailablePets_ReturnsPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        await CreatePetAsync("DiscoverTest1", petTypeId);
        await CreatePetAsync("DiscoverTest2", petTypeId);

        // Act
        var response = await _client.GetAsync("/api/discover?take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        body!.Pets.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Discover_ExcludesFavoritedPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId1 = await CreatePetAsync("FavExclude1", petTypeId);
        var petId2 = await CreatePetAsync("FavExclude2", petTypeId);

        // Favorite pet1
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId1 });

        // Act
        var response = await _client.GetAsync("/api/discover?take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().NotContain(petId1);
        petIds.Should().Contain(petId2);
    }

    [Fact]
    public async Task Discover_ExcludesSkippedPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId1 = await CreatePetAsync("SkipExclude1", petTypeId);
        var petId2 = await CreatePetAsync("SkipExclude2", petTypeId);

        // Skip pet1
        await _client.PostAsJsonAsync("/api/skips", new { PetId = petId1 });

        // Act
        var response = await _client.GetAsync("/api/discover?take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().NotContain(petId1);
        petIds.Should().Contain(petId2);
    }

    [Fact]
    public async Task Discover_WhenAllPetsSeen_ReturnsEmpty()
    {
        // Arrange -- use a unique user to avoid interference from other tests
        var uniqueUserId = Guid.NewGuid().ToString();
        var uniqueClient = _factory.CreateClient();
        uniqueClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: uniqueUserId));

        var petTypeId = await SeedPetTypeAsync();

        // Get all discoverable pets and skip/favorite them all
        var initialResponse = await uniqueClient.GetAsync("/api/discover?take=1000");
        var initialBody = await initialResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();

        foreach (var pet in initialBody!.Pets)
        {
            await uniqueClient.PostAsJsonAsync("/api/skips", new { PetId = pet.Id });
        }

        // Act
        var response = await uniqueClient.GetAsync("/api/discover?take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        body!.Pets.Should().BeEmpty();
        body.HasMore.Should().BeFalse();

        uniqueClient.Dispose();
    }

    [Fact]
    public async Task Discover_WithPetTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        var dogTypeId = await SeedPetTypeAsync("dog", "Dog");
        var catTypeId = await SeedPetTypeAsync("cat", "Cat");
        var dogPetId = await CreatePetAsync("FilterDog", dogTypeId);
        var catPetId = await CreatePetAsync("FilterCat", catTypeId);

        // Act -- filter by cat type only
        var response = await _client.GetAsync($"/api/discover?petTypeId={catTypeId}&take=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        body.Should().NotBeNull();
        var petIds = body!.Pets.Select(p => p.Id).ToList();
        petIds.Should().Contain(catPetId);
        petIds.Should().NotContain(dogPetId);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/skips (Track Skip)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrackSkip_WithValidPet_ReturnsCreated()
    {
        // Arrange
        var petId = await CreatePetAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TrackSkipResponseDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.PetId.Should().Be(petId);
    }

    [Fact]
    public async Task TrackSkip_Duplicate_ReturnsConflict()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TrackSkip_NonExistentPet_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/skips", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // DELETE /api/skips (Reset Skips)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetSkips_AfterSkipping_PetsReappearInDiscovery()
    {
        // Arrange
        var uniqueUserId = Guid.NewGuid().ToString();
        var uniqueClient = _factory.CreateClient();
        uniqueClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: uniqueUserId));

        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreatePetAsync("ResetTest", petTypeId);

        // Skip the pet
        await uniqueClient.PostAsJsonAsync("/api/skips", new { PetId = petId });

        // Verify it's excluded
        var beforeResponse = await uniqueClient.GetAsync("/api/discover?take=100");
        var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        beforeBody!.Pets.Select(p => p.Id).Should().NotContain(petId);

        // Act -- reset skips
        var resetResponse = await uniqueClient.DeleteAsync("/api/skips");

        // Assert
        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResponse = await uniqueClient.GetAsync("/api/discover?take=100");
        var afterBody = await afterResponse.Content.ReadFromJsonAsync<DiscoverResponseDto>();
        afterBody!.Pets.Select(p => p.Id).Should().Contain(petId);

        uniqueClient.Dispose();
    }

    // ──────────────────────────────────────────────────────────────
    // Auth
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Discover_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/discover");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrackSkip_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/skips", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetSkips_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/skips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponseDto(Guid Id);
    private record CreatePetTypeResponseDto(Guid Id, string Code, string Name);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record DiscoverPetDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);
    private record DiscoverResponseDto(List<DiscoverPetDto> Pets, bool HasMore);
    private record TrackSkipResponseDto(Guid Id, Guid PetId, DateTime CreatedAt);
}
```

- [ ] **Step 2: Run integration tests**

```bash
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests
```

Expected: All existing tests pass + all new discover/skip tests pass.

- [ ] **Step 3: Commit**

```
add integration tests for discovery algorithm, skip tracking, and skip reset
```

---

## Chunk 6: Frontend -- Update Discover Page

### Task 6.1: Add API models for discovery

**File:** `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` (modify)

- [ ] **Step 1: Add DiscoverPetsResponse model**

Add after the `PetsResponse` record:

```csharp
public record DiscoverPetsResponse(IEnumerable<PetListItem> Pets, bool HasMore);
```

### Task 6.2: Add discovery methods to PetApiClient

**File:** `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` (modify)

- [ ] **Step 2: Add GetDiscoverPetsAsync, TrackSkipAsync, ResetSkipsAsync**

Add the following methods to `PetApiClient`:

```csharp
public async Task<DiscoverPetsResponse?> GetDiscoverPetsAsync(Guid? petTypeId = null, int? minAge = null, int? maxAge = null, int take = 10)
{
    var query = $"api/discover?take={take}";
    if (petTypeId.HasValue) query += $"&petTypeId={petTypeId}";
    if (minAge.HasValue) query += $"&minAge={minAge}";
    if (maxAge.HasValue) query += $"&maxAge={maxAge}";
    return await _http.GetFromJsonAsync<DiscoverPetsResponse>(query);
}

public Task<HttpResponseMessage> TrackSkipAsync(Guid petId) =>
    _http.PostAsJsonAsync("api/skips", new { PetId = petId });

public Task<HttpResponseMessage> ResetSkipsAsync() =>
    _http.DeleteAsync("api/skips");
```

### Task 6.3: Update Discover.razor

**File:** `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor` (modify)

- [ ] **Step 3: Replace Discover.razor with discovery algorithm flow**

Replace the entire file with:

```razor
@page "/discover"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject ISnackbar Snackbar
@inject NavigationManager Nav

<PageTitle>Discover Pets</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudSelect T="Guid?" Label="Filter by Pet Type" @bind-Value="_selectedTypeId" @bind-Value:after="OnFilterChanged" Class="mb-4" Clearable="true">
        @if (_petTypes is not null)
        {
            @foreach (var pt in _petTypes)
            {
                <MudSelectItem Value="@((Guid?)pt.Id)">@pt.Name</MudSelectItem>
            }
        }
    </MudSelect>

    @if (_loading)
    {
        <div class="d-flex justify-center mt-8">
            <MudSkeleton SkeletonType="SkeletonType.Rectangle" Width="300px" Height="420px" />
        </div>
    }
    else if (!_pets.Any())
    {
        <div class="d-flex flex-column align-center mt-8 gap-4">
            <MudIcon Icon="@Icons.Material.Filled.Pets" Size="Size.Large" Color="Color.Secondary" />
            <MudText Typo="Typo.h6" Align="Align.Center" Color="Color.Secondary">
                You've seen all available pets matching your filters!
            </MudText>
            <div class="d-flex gap-2">
                <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="ResetAndReload">
                    Reset and discover again
                </MudButton>
                <MudButton Variant="Variant.Outlined" Color="Color.Secondary" OnClick="@(() => Nav.NavigateTo("/favorites"))">
                    View favorites
                </MudButton>
            </div>
        </div>
    }
    else
    {
        <div class="d-flex justify-center">
            <SwipeCardStack Pets="_pets" OnFavorite="HandleFavorite" OnSkip="HandleSkip" OnNeedMore="LoadMore" />
        </div>
    }
</MudContainer>

@code {
    private List<PetListItem> _pets = [];
    private List<PetTypeItem>? _petTypes;
    private Guid? _selectedTypeId;
    private bool _loading = true;
    private bool _hasMore = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPetTypes();
        await LoadPets();
    }

    private async Task LoadPetTypes()
    {
        try
        {
            var response = await PetApi.GetPetTypesAsync();
            _petTypes = response?.Items?.Where(pt => pt.IsActive).ToList();
        }
        catch { }
    }

    private async Task LoadPets()
    {
        _loading = true;
        _pets.Clear();
        _hasMore = true;
        await FetchBatch();
        _loading = false;
    }

    private async Task FetchBatch()
    {
        if (!_hasMore) return;
        try
        {
            var response = await PetApi.GetDiscoverPetsAsync(
                petTypeId: _selectedTypeId,
                take: 10);

            if (response?.Pets is not null)
            {
                var newPets = response.Pets.ToList();
                _pets.AddRange(newPets);
                _hasMore = response.HasMore;
            }
        }
        catch
        {
            Snackbar.Add("Failed to load pets", Severity.Error);
        }
    }

    private async Task HandleFavorite(PetListItem pet)
    {
        try
        {
            var response = await PetApi.AddFavoriteAsync(pet.Id);
            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Failed to add favorite", Severity.Error);
            }
        }
        catch
        {
            Snackbar.Add("Connection error", Severity.Error);
        }
    }

    private async Task HandleSkip(PetListItem pet)
    {
        try
        {
            var response = await PetApi.TrackSkipAsync(pet.Id);
            if (!response.IsSuccessStatusCode)
            {
                // Non-critical -- skip tracking failure shouldn't block the UX
                Snackbar.Add("Failed to track skip", Severity.Warning);
            }
        }
        catch
        {
            // Silently fail -- skip tracking is best-effort from UX perspective
        }
    }

    private async Task LoadMore() => await FetchBatch();

    private async Task OnFilterChanged() => await LoadPets();

    private async Task ResetAndReload()
    {
        try
        {
            var response = await PetApi.ResetSkipsAsync();
            if (response.IsSuccessStatusCode)
            {
                await LoadPets();
                Snackbar.Add("Skips reset! Discover pets again.", Severity.Success);
            }
            else
            {
                Snackbar.Add("Failed to reset skips", Severity.Error);
            }
        }
        catch
        {
            Snackbar.Add("Connection error", Severity.Error);
        }
    }
}
```

- [ ] **Step 4: Build frontend to verify**

```bash
dotnet build src/Web/PetAdoption.Web.BlazorApp
```

- [ ] **Step 5: Run all tests**

```bash
dotnet test PetAdoption.sln
```

- [ ] **Step 6: Commit**

```
update Discover page to use personalized discovery feed with skip tracking and empty state
```

---

## Summary

### New API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/discover?petTypeId=&minAge=&maxAge=&take=10` | Authenticated | Personalized discovery feed (excludes favorites + skips) |
| POST | `/api/skips` | Authenticated | Track a pet skip `{petId}` |
| DELETE | `/api/skips` | Authenticated | Reset all user's skips |

### New Domain Entity

| Entity | Table | Indexes |
|--------|-------|---------|
| `PetSkip` | `PetSkips` | Unique compound `(UserId, PetId)` |

### Core Algorithm (GetDiscoverPetsQueryHandler)

1. Fetch user's favorited pet IDs from `Favorites` table
2. Fetch user's skipped pet IDs from `PetSkips` table
3. Union both sets into an exclusion list
4. Query `Pets` where `Status == Available` AND `Id NOT IN exclusionList`
5. Apply optional filters: `petTypeId`, `minAgeMonths`, `maxAgeMonths`
6. Fetch `take + 1` rows to determine `HasMore`
7. Map to DTOs and return

### User Flow (After Implementation)

1. User opens `/discover`
2. Frontend calls `GET /api/discover?take=10`
3. Backend returns pets the user hasn't seen, excluding favorites + skips
4. User swipes right (like) --> `POST /api/favorites` (existing) -- pet excluded from future discovery
5. User swipes left (skip) --> `POST /api/skips` (new) -- pet excluded from future discovery
6. When 3 cards remain --> `LoadMore` fetches next batch via `GET /api/discover`
7. When no pets remain --> Empty state with "Reset and discover again", "View favorites" buttons
8. "Reset and discover again" --> `DELETE /api/skips` + reload -- skipped pets reappear (favorites stay excluded)
