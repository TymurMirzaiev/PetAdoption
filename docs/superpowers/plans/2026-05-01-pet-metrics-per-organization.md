# Pet Metrics per Organization Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Track pet interaction metrics (impressions, swipes, rejections) and expose per-organization analytics dashboards for OrgAdmin/OrgModerator roles.

**Architecture:** Adds a `PetInteraction` event-log entity to PetService Domain, with `IPetInteractionRepository` (write) and `IPetMetricsQueryStore` (read) following existing CQRS separation. A new `InteractionsController` handles tracking calls from the Blazor frontend, and an `OrganizationMetricsController` serves aggregated metrics computed via SQL GROUP BY queries. The Blazor frontend gets a new `/org/{orgId}/metrics` page using MudDataGrid.

**Tech Stack:** .NET 9.0, EF Core + SQL Server, custom mediator, Blazor WASM + MudBlazor 8.x, xUnit + FluentAssertions + Testcontainers

**Dependencies:** Plan 4 (Organization Management + Platform Admin) must be completed first. Pet must have an `OrganizationId` property.

---

## File Structure

### New files:
- `src/Services/PetService/PetAdoption.PetService.Domain/PetInteraction.cs` — PetInteraction entity + InteractionType enum
- `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetInteractionRepository.cs` — Write repository interface
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetMetricsQueryStore.cs` — Read query store interface + PetMetricsSummary DTO
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommand.cs` — Single interaction command
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommandHandler.cs` — Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommand.cs` — Batch impressions command
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommandHandler.cs` — Batch handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgMetricsQuery.cs` — Org metrics query + handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetMetricsQuery.cs` — Single pet metrics query + handler + response
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetInteractionRepository.cs` — EF Core write implementation
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetMetricsQueryStore.cs` — EF Core read implementation with SQL aggregation
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/InteractionsController.cs` — Track interactions endpoints
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/OrganizationMetricsController.cs` — Metrics query endpoints
- `src/Web/PetAdoption.Web.BlazorApp/Pages/Org/OrgMetrics.razor` — Organization metrics dashboard page
- `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetInteractionTests.cs` — Domain unit tests
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/InteractionsControllerTests.cs` — Integration tests for tracking
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/OrganizationMetricsControllerTests.cs` — Integration tests for metrics

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` — Add DbSet&lt;PetInteraction&gt;, entity config with indexes
- `src/Services/PetService/PetAdoption.PetService.API/Program.cs` — Register IPetInteractionRepository, IPetMetricsQueryStore
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor` — Track impressions on load, track rejections on skip
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` — Add tracking and metrics API methods
- `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` — Add metrics model records

---

## Chunk 1: Domain Layer — PetInteraction Entity

### Task 1: Create PetInteraction entity and InteractionType enum

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/PetInteraction.cs`

- [ ] **Step 1: Create PetInteraction entity with factory method**

```csharp
namespace PetAdoption.PetService.Domain;

public enum InteractionType
{
    Impression = 0,
    Swipe = 1,
    Rejection = 2
}

public class PetInteraction
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid UserId { get; private set; }
    public InteractionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PetInteraction() { }

    public static PetInteraction Create(Guid petId, Guid userId, InteractionType type)
    {
        if (petId == Guid.Empty)
            throw new ArgumentException("PetId cannot be empty.", nameof(petId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new PetInteraction
        {
            Id = Guid.NewGuid(),
            PetId = petId,
            UserId = userId,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 2: Run existing tests to verify no regressions**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/PetInteraction.cs
git commit -m "add PetInteraction entity with InteractionType enum"
```

---

### Task 2: Unit tests for PetInteraction

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetInteractionTests.cs`

- [ ] **Step 1: Create PetInteraction unit tests**

```csharp
using FluentAssertions;
using PetAdoption.PetService.Domain;
using Xunit;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetInteractionTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InteractionType.Impression)]
    [InlineData(InteractionType.Swipe)]
    [InlineData(InteractionType.Rejection)]
    public void Create_WithValidData_ShouldCreateInteraction(InteractionType type)
    {
        // Arrange
        var petId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var interaction = PetInteraction.Create(petId, userId, type);

        // Assert
        interaction.Id.Should().NotBeEmpty();
        interaction.PetId.Should().Be(petId);
        interaction.UserId.Should().Be(userId);
        interaction.Type.Should().Be(type);
        interaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetInteraction.Create(Guid.Empty, Guid.NewGuid(), InteractionType.Impression);
        act.Should().Throw<ArgumentException>().WithParameterName("petId");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetInteraction.Create(Guid.NewGuid(), Guid.Empty, InteractionType.Impression);
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }
}
```

- [ ] **Step 2: Run unit tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`

- [ ] **Step 3: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetInteractionTests.cs
git commit -m "add unit tests for PetInteraction entity"
```

---

## Chunk 2: Domain + Application Interfaces

### Task 3: Create IPetInteractionRepository (write interface)

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetInteractionRepository.cs`

- [ ] **Step 1: Create write repository interface**

```csharp
namespace PetAdoption.PetService.Domain.Interfaces;

public interface IPetInteractionRepository
{
    Task AddAsync(PetInteraction interaction);
    Task AddBatchAsync(IEnumerable<PetInteraction> interactions);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IPetInteractionRepository.cs
git commit -m "add IPetInteractionRepository write interface"
```

---

### Task 4: Create IPetMetricsQueryStore (read interface) and PetMetricsSummary DTO

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetMetricsQueryStore.cs`

- [ ] **Step 1: Create query store interface with DTO**

```csharp
namespace PetAdoption.PetService.Application.Queries;

public record PetMetricsSummary(
    Guid PetId,
    string PetName,
    string PetType,
    long ImpressionCount,
    long SwipeCount,
    long RejectionCount,
    long FavoriteCount,
    double SwipeRate,
    double RejectionRate);

public interface IPetMetricsQueryStore
{
    Task<IEnumerable<PetMetricsSummary>> GetMetricsByOrgAsync(
        Guid orgId, DateTime? from, DateTime? to, string? sortBy, bool descending);
    Task<PetMetricsSummary?> GetMetricsByPetAsync(Guid petId, DateTime? from, DateTime? to);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetMetricsQueryStore.cs
git commit -m "add IPetMetricsQueryStore read interface and PetMetricsSummary dto"
```

---

## Chunk 3: Application Layer — Commands

### Task 5: TrackInteractionCommand (single interaction)

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommandHandler.cs`

- [ ] **Step 1: Create TrackInteractionCommand**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Commands;

public record TrackInteractionCommand(
    Guid PetId,
    Guid UserId,
    InteractionType Type) : IRequest<TrackInteractionResponse>;
```

- [ ] **Step 2: Create TrackInteractionCommandHandler with response**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record TrackInteractionResponse(Guid Id);

public class TrackInteractionCommandHandler : IRequestHandler<TrackInteractionCommand, TrackInteractionResponse>
{
    private readonly IPetInteractionRepository _repository;

    public TrackInteractionCommandHandler(IPetInteractionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrackInteractionResponse> Handle(TrackInteractionCommand request, CancellationToken ct = default)
    {
        var interaction = PetInteraction.Create(request.PetId, request.UserId, request.Type);
        await _repository.AddAsync(interaction);
        return new TrackInteractionResponse(interaction.Id);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommandHandler.cs
git commit -m "add TrackInteractionCommand and handler"
```

---

### Task 6: TrackBatchImpressionsCommand

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommandHandler.cs`

- [ ] **Step 1: Create TrackBatchImpressionsCommand**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record TrackBatchImpressionsCommand(
    IEnumerable<Guid> PetIds,
    Guid UserId) : IRequest<TrackBatchImpressionsResponse>;
```

- [ ] **Step 2: Create TrackBatchImpressionsCommandHandler with response**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record TrackBatchImpressionsResponse(int Count);

public class TrackBatchImpressionsCommandHandler
    : IRequestHandler<TrackBatchImpressionsCommand, TrackBatchImpressionsResponse>
{
    private readonly IPetInteractionRepository _repository;

    public TrackBatchImpressionsCommandHandler(IPetInteractionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrackBatchImpressionsResponse> Handle(
        TrackBatchImpressionsCommand request, CancellationToken ct = default)
    {
        var interactions = request.PetIds
            .Select(petId => PetInteraction.Create(petId, request.UserId, InteractionType.Impression))
            .ToList();

        await _repository.AddBatchAsync(interactions);
        return new TrackBatchImpressionsResponse(interactions.Count);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommandHandler.cs
git commit -m "add TrackBatchImpressionsCommand and handler"
```

---

## Chunk 4: Application Layer — Queries

### Task 7: GetOrgMetricsQuery

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgMetricsQuery.cs`

- [ ] **Step 1: Create query, response, and handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgMetricsQuery(
    Guid OrgId,
    DateTime? From,
    DateTime? To,
    string? SortBy,
    bool Descending = true) : IRequest<GetOrgMetricsResponse>;

public record GetOrgMetricsResponse(IEnumerable<PetMetricsSummary> Metrics);

public class GetOrgMetricsQueryHandler : IRequestHandler<GetOrgMetricsQuery, GetOrgMetricsResponse>
{
    private readonly IPetMetricsQueryStore _queryStore;

    public GetOrgMetricsQueryHandler(IPetMetricsQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetOrgMetricsResponse> Handle(GetOrgMetricsQuery request, CancellationToken ct = default)
    {
        var metrics = await _queryStore.GetMetricsByOrgAsync(
            request.OrgId, request.From, request.To, request.SortBy, request.Descending);
        return new GetOrgMetricsResponse(metrics);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgMetricsQuery.cs
git commit -m "add GetOrgMetricsQuery and handler"
```

---

### Task 8: GetPetMetricsQuery

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetMetricsQuery.cs`

- [ ] **Step 1: Create query, response, and handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetMetricsQuery(
    Guid PetId,
    DateTime? From,
    DateTime? To) : IRequest<GetPetMetricsResponse>;

public record GetPetMetricsResponse(PetMetricsSummary Metrics);

public class GetPetMetricsQueryHandler : IRequestHandler<GetPetMetricsQuery, GetPetMetricsResponse>
{
    private readonly IPetMetricsQueryStore _queryStore;

    public GetPetMetricsQueryHandler(IPetMetricsQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetPetMetricsResponse> Handle(GetPetMetricsQuery request, CancellationToken ct = default)
    {
        var metrics = await _queryStore.GetMetricsByPetAsync(request.PetId, request.From, request.To)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");
        return new GetPetMetricsResponse(metrics);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetMetricsQuery.cs
git commit -m "add GetPetMetricsQuery and handler"
```

---

## Chunk 5: Infrastructure Layer — Persistence

### Task 9: Add PetInteraction to PetServiceDbContext

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs`

- [ ] **Step 1: Add DbSet and entity configuration**

Add `DbSet<PetInteraction>` property:

```csharp
public DbSet<PetInteraction> PetInteractions => Set<PetInteraction>();
```

Add entity configuration inside `OnModelCreating` after the `OutboxEvent` block:

```csharp
modelBuilder.Entity<PetInteraction>(entity =>
{
    entity.ToTable("PetInteractions");
    entity.HasKey(pi => pi.Id);
    entity.Property(pi => pi.Type).HasConversion<int>();
    entity.HasIndex(pi => new { pi.PetId, pi.Type });
    entity.HasIndex(pi => pi.CreatedAt);
    entity.HasIndex(pi => pi.PetId);
});
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs
git commit -m "add PetInteractions table with indexes to PetServiceDbContext"
```

---

### Task 10: Implement PetInteractionRepository

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetInteractionRepository.cs`

- [ ] **Step 1: Create EF Core repository implementation**

```csharp
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetInteractionRepository : IPetInteractionRepository
{
    private readonly PetServiceDbContext _db;

    public PetInteractionRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PetInteraction interaction)
    {
        _db.PetInteractions.Add(interaction);
        await _db.SaveChangesAsync();
    }

    public async Task AddBatchAsync(IEnumerable<PetInteraction> interactions)
    {
        _db.PetInteractions.AddRange(interactions);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetInteractionRepository.cs
git commit -m "implement PetInteractionRepository with EF Core"
```

---

### Task 11: Implement PetMetricsQueryStore

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetMetricsQueryStore.cs`

- [ ] **Step 1: Create PetMetricsQueryStore with SQL aggregation**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetMetricsQueryStore : IPetMetricsQueryStore
{
    private readonly PetServiceDbContext _db;

    public PetMetricsQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PetMetricsSummary>> GetMetricsByOrgAsync(
        Guid orgId, DateTime? from, DateTime? to, string? sortBy, bool descending)
    {
        var petIds = await _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Select(p => p.Id)
            .ToListAsync();

        if (!petIds.Any())
            return Enumerable.Empty<PetMetricsSummary>();

        var metrics = await BuildMetricsQuery(petIds, from, to);
        return SortMetrics(metrics, sortBy, descending);
    }

    public async Task<PetMetricsSummary?> GetMetricsByPetAsync(
        Guid petId, DateTime? from, DateTime? to)
    {
        var pet = await _db.Pets.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId);

        if (pet is null) return null;

        var metrics = await BuildMetricsQuery(new List<Guid> { petId }, from, to);
        return metrics.FirstOrDefault();
    }

    private async Task<List<PetMetricsSummary>> BuildMetricsQuery(
        List<Guid> petIds, DateTime? from, DateTime? to)
    {
        var interactionsQuery = _db.PetInteractions.AsNoTracking()
            .Where(pi => petIds.Contains(pi.PetId));

        if (from.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt >= from.Value);
        if (to.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt <= to.Value);

        var interactionCounts = await interactionsQuery
            .GroupBy(pi => new { pi.PetId, pi.Type })
            .Select(g => new
            {
                g.Key.PetId,
                g.Key.Type,
                Count = g.LongCount()
            })
            .ToListAsync();

        var favoriteCounts = await _db.Favorites.AsNoTracking()
            .Where(f => petIds.Contains(f.PetId))
            .GroupBy(f => f.PetId)
            .Select(g => new { PetId = g.Key, Count = g.LongCount() })
            .ToListAsync();

        var favoriteDict = favoriteCounts.ToDictionary(x => x.PetId, x => x.Count);

        var pets = await _db.Pets.AsNoTracking()
            .Where(p => petIds.Contains(p.Id))
            .Join(_db.PetTypes.AsNoTracking(), p => p.PetTypeId, pt => pt.Id,
                (p, pt) => new { p.Id, PetName = p.Name.Value, PetType = pt.Name })
            .ToListAsync();

        var petDict = pets.ToDictionary(p => p.Id);

        var result = new List<PetMetricsSummary>();
        foreach (var petId in petIds)
        {
            if (!petDict.TryGetValue(petId, out var petInfo)) continue;

            var impressions = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Impression)
                .Sum(x => x.Count);
            var swipes = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Swipe)
                .Sum(x => x.Count);
            var rejections = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Rejection)
                .Sum(x => x.Count);
            var favorites = favoriteDict.GetValueOrDefault(petId, 0);

            var swipeRate = impressions > 0 ? (double)swipes / impressions : 0;
            var rejectionRate = impressions > 0 ? (double)rejections / impressions : 0;

            result.Add(new PetMetricsSummary(
                petId, petInfo.PetName, petInfo.PetType,
                impressions, swipes, rejections, favorites,
                Math.Round(swipeRate, 4), Math.Round(rejectionRate, 4)));
        }

        return result;
    }

    private static IEnumerable<PetMetricsSummary> SortMetrics(
        List<PetMetricsSummary> metrics, string? sortBy, bool descending)
    {
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "impressions" => descending
                ? metrics.OrderByDescending(m => m.ImpressionCount)
                : metrics.OrderBy(m => m.ImpressionCount),
            "swipes" => descending
                ? metrics.OrderByDescending(m => m.SwipeCount)
                : metrics.OrderBy(m => m.SwipeCount),
            "rejections" => descending
                ? metrics.OrderByDescending(m => m.RejectionCount)
                : metrics.OrderBy(m => m.RejectionCount),
            "favorites" => descending
                ? metrics.OrderByDescending(m => m.FavoriteCount)
                : metrics.OrderBy(m => m.FavoriteCount),
            "swiperate" => descending
                ? metrics.OrderByDescending(m => m.SwipeRate)
                : metrics.OrderBy(m => m.SwipeRate),
            "rejectionrate" => descending
                ? metrics.OrderByDescending(m => m.RejectionRate)
                : metrics.OrderBy(m => m.RejectionRate),
            "name" => descending
                ? metrics.OrderByDescending(m => m.PetName)
                : metrics.OrderBy(m => m.PetName),
            _ => descending
                ? metrics.OrderByDescending(m => m.ImpressionCount)
                : metrics.OrderBy(m => m.ImpressionCount)
        };

        return sorted.ToList();
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetMetricsQueryStore.cs
git commit -m "implement PetMetricsQueryStore with SQL aggregation"
```

---

## Chunk 6: DI Registration

### Task 12: Register new services in Program.cs

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Program.cs`

- [ ] **Step 1: Add repository and query store registrations**

Add after the existing `IAnnouncementQueryStore` registration (around line 51):

```csharp
builder.Services.AddScoped<IPetInteractionRepository, PetInteractionRepository>();
builder.Services.AddScoped<IPetMetricsQueryStore, PetMetricsQueryStore>();
```

Note: Handlers are auto-discovered by the mediator reflection-based registration — no explicit handler registration needed.

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Program.cs
git commit -m "register PetInteractionRepository and PetMetricsQueryStore in DI"
```

---

## Chunk 7: API Layer — Controllers

### Task 13: Create InteractionsController

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.API/Controllers/InteractionsController.cs`

- [ ] **Step 1: Create controller with track and batch endpoints**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class InteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    [HttpPost("api/pets/{petId:guid}/interactions")]
    public async Task<IActionResult> TrackInteraction(Guid petId, [FromBody] TrackInteractionRequest request)
    {
        if (!Enum.TryParse<InteractionType>(request.Type, true, out var type))
            return BadRequest(new { error = "Invalid interaction type. Use: Impression, Swipe, or Rejection." });

        var result = await _mediator.Send(new TrackInteractionCommand(petId, GetUserId(), type));
        return Ok(result);
    }

    [HttpPost("api/pets/interactions/batch")]
    public async Task<IActionResult> TrackBatchImpressions([FromBody] TrackBatchImpressionsRequest request)
    {
        if (request.PetIds is null || !request.PetIds.Any())
            return BadRequest(new { error = "PetIds cannot be empty." });

        var result = await _mediator.Send(new TrackBatchImpressionsCommand(request.PetIds, GetUserId()));
        return Ok(result);
    }
}

public record TrackInteractionRequest(string Type);
public record TrackBatchImpressionsRequest(IEnumerable<Guid> PetIds);
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/InteractionsController.cs
git commit -m "add InteractionsController for tracking pet interactions"
```

---

### Task 14: Create OrganizationMetricsController

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.API/Controllers/OrganizationMetricsController.cs`

- [ ] **Step 1: Create controller with org and pet metrics endpoints**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class OrganizationMetricsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationMetricsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/organizations/{orgId:guid}/metrics")]
    [Authorize(Policy = "AdminOnly")] // TODO: Replace with org-level auth once Plan 4 roles exist
    public async Task<IActionResult> GetOrgMetrics(
        Guid orgId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true)
    {
        var result = await _mediator.Send(new GetOrgMetricsQuery(orgId, from, to, sortBy, descending));
        return Ok(result);
    }

    [HttpGet("api/pets/{petId:guid}/metrics")]
    public async Task<IActionResult> GetPetMetrics(
        Guid petId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _mediator.Send(new GetPetMetricsQuery(petId, from, to));
        return Ok(result);
    }
}
```

- [ ] **Step 2: Build full solution**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/OrganizationMetricsController.cs
git commit -m "add OrganizationMetricsController for org and pet metrics"
```

---

## Chunk 8: Blazor Frontend — API Client + Models

### Task 15: Add API models and PetApiClient methods

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs`
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`

- [ ] **Step 1: Add new model records to ApiModels.cs**

Append to the end of `ApiModels.cs`:

```csharp
public record PetMetricsSummaryItem(
    Guid PetId, string PetName, string PetType,
    long ImpressionCount, long SwipeCount, long RejectionCount,
    long FavoriteCount, double SwipeRate, double RejectionRate);
public record OrgMetricsResponse(IEnumerable<PetMetricsSummaryItem> Metrics);
public record PetMetricsDetailResponse(PetMetricsSummaryItem Metrics);
```

- [ ] **Step 2: Add API client methods to PetApiClient.cs**

```csharp
public Task<HttpResponseMessage> TrackInteractionAsync(Guid petId, string type) =>
    _http.PostAsJsonAsync($"api/pets/{petId}/interactions", new { Type = type });

public Task<HttpResponseMessage> TrackBatchImpressionsAsync(IEnumerable<Guid> petIds) =>
    _http.PostAsJsonAsync("api/pets/interactions/batch", new { PetIds = petIds });

public async Task<OrgMetricsResponse?> GetOrgMetricsAsync(
    Guid orgId, DateTime? from = null, DateTime? to = null,
    string? sortBy = null, bool descending = true)
{
    var query = $"api/organizations/{orgId}/metrics?descending={descending}";
    if (from.HasValue) query += $"&from={from.Value:yyyy-MM-ddTHH:mm:ss}";
    if (to.HasValue) query += $"&to={to.Value:yyyy-MM-ddTHH:mm:ss}";
    if (sortBy is not null) query += $"&sortBy={sortBy}";
    return await _http.GetFromJsonAsync<OrgMetricsResponse>(query);
}

public async Task<PetMetricsDetailResponse?> GetPetMetricsAsync(Guid petId)
{
    return await _http.GetFromJsonAsync<PetMetricsDetailResponse>($"api/pets/{petId}/metrics");
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs
git commit -m "add metrics API models and PetApiClient methods"
```

---

## Chunk 9: Blazor Frontend — Discover Page Tracking

### Task 16: Update Discover.razor to track impressions and rejections

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor`

- [ ] **Step 1: Track batch impressions when pets are loaded**

In the `FetchBatch()` method, after adding new pets, fire-and-forget impression tracking:

```csharp
private async Task FetchBatch()
{
    if (!_hasMore) return;
    try
    {
        var response = await PetApi.GetPetsAsync(status: "Available", petTypeId: _selectedTypeId, skip: _skip, take: 10);
        if (response?.Pets is not null)
        {
            var newPets = response.Pets.ToList();
            _pets.AddRange(newPets);
            _skip += newPets.Count;
            _hasMore = newPets.Count == 10;

            // Fire-and-forget: track impressions for loaded pets
            if (newPets.Any())
            {
                _ = PetApi.TrackBatchImpressionsAsync(newPets.Select(p => p.Id));
            }
        }
    }
    catch
    {
        Snackbar.Add("Failed to load pets", Severity.Error);
    }
}
```

- [ ] **Step 2: Track rejection on HandleSkip and swipe on HandleFavorite**

```csharp
private Task HandleSkip(PetListItem pet)
{
    _ = PetApi.TrackInteractionAsync(pet.Id, "Rejection");
    return Task.CompletedTask;
}

private async Task HandleFavorite(PetListItem pet)
{
    _ = PetApi.TrackInteractionAsync(pet.Id, "Swipe");

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
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor
git commit -m "track impressions, swipes, and rejections from Discover page"
```

---

## Chunk 10: Blazor Frontend — Organization Metrics Dashboard

### Task 17: Create OrgMetrics page

**Files:**
- Create: `src/Web/PetAdoption.Web.BlazorApp/Pages/Org/OrgMetrics.razor`

- [ ] **Step 1: Create organization metrics dashboard page**

```razor
@page "/org/{OrgId:guid}/metrics"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject ISnackbar Snackbar

<PageTitle>Organization Metrics</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Pet Metrics</MudText>

    <MudGrid Class="mb-4">
        <MudItem xs="12" sm="4">
            <MudDatePicker Label="From" @bind-Date="_fromDate" />
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudDatePicker Label="To" @bind-Date="_toDate" />
        </MudItem>
        <MudItem xs="12" sm="4" Class="d-flex align-end">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="LoadMetrics">Apply Filter</MudButton>
        </MudItem>
    </MudGrid>

    @if (_loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (_metrics is null || !_metrics.Any())
    {
        <MudText Typo="Typo.body1" Color="Color.Secondary">No metrics data available.</MudText>
    }
    else
    {
        <MudDataGrid Items="@_metrics" SortMode="SortMode.Single" Dense="true" Hover="true" Bordered="true">
            <Columns>
                <PropertyColumn Property="x => x.PetName" Title="Pet Name" />
                <PropertyColumn Property="x => x.PetType" Title="Type" />
                <PropertyColumn Property="x => x.ImpressionCount" Title="Impressions" />
                <PropertyColumn Property="x => x.SwipeCount" Title="Swipes" />
                <PropertyColumn Property="x => x.RejectionCount" Title="Rejections" />
                <PropertyColumn Property="x => x.FavoriteCount" Title="Favorites" />
                <TemplateColumn Title="Swipe Rate">
                    <CellTemplate>
                        <MudText>@($"{context.Item.SwipeRate:P1}")</MudText>
                    </CellTemplate>
                </TemplateColumn>
                <TemplateColumn Title="Rejection Rate">
                    <CellTemplate>
                        <MudText>@($"{context.Item.RejectionRate:P1}")</MudText>
                    </CellTemplate>
                </TemplateColumn>
            </Columns>
        </MudDataGrid>

        <MudGrid Class="mt-6">
            <MudItem xs="12" sm="4">
                <MudPaper Class="pa-4 text-center" Elevation="3">
                    <MudText Typo="Typo.h5">@_metrics.Sum(m => m.ImpressionCount)</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">Total Impressions</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="12" sm="4">
                <MudPaper Class="pa-4 text-center" Elevation="3">
                    <MudText Typo="Typo.h5">@_metrics.Sum(m => m.SwipeCount)</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">Total Swipes</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="12" sm="4">
                <MudPaper Class="pa-4 text-center" Elevation="3">
                    <MudText Typo="Typo.h5">@_metrics.Sum(m => m.FavoriteCount)</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">Total Favorites</MudText>
                </MudPaper>
            </MudItem>
        </MudGrid>
    }
</MudContainer>

@code {
    [Parameter] public Guid OrgId { get; set; }

    private List<PetMetricsSummaryItem>? _metrics;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadMetrics();
    }

    private async Task LoadMetrics()
    {
        _loading = true;
        try
        {
            var response = await PetApi.GetOrgMetricsAsync(OrgId, _fromDate, _toDate);
            _metrics = response?.Metrics?.ToList();
        }
        catch
        {
            Snackbar.Add("Failed to load metrics", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/Org/OrgMetrics.razor
git commit -m "add organization metrics dashboard page with MudDataGrid"
```

---

## Chunk 11: Full Test Run + Final Verification

### Task 18: Run all tests and verify

- [ ] **Step 1: Run full unit test suite**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`

- [ ] **Step 2: Run full integration test suite**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`

- [ ] **Step 3: Build entire solution**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 4: Final commit**

```bash
git add docs/superpowers/plans/2026-05-01-pet-metrics-per-organization.md
git commit -m "add pet metrics per organization implementation plan"
```

---

## Design Decisions

### Why PetInteraction is not an aggregate
PetInteraction is a simple event log entity — no domain behavior, no state transitions, no invariants. It is append-only, similar to Favorite but even simpler.

### Why not domain events for interactions
Interaction tracking is high-volume, low-value. Using domain events and the outbox pattern would add unnecessary overhead. Direct inserts are appropriate here.

### Performance at scale
The current implementation computes metrics on-the-fly with GROUP BY queries. For a production system with millions of interactions, consider a materialized `PetMetricsSnapshot` table refreshed by a periodic background job. The composite index on `(PetId, Type)` ensures efficient aggregation.

### Fire-and-forget tracking from Blazor
Tracking calls from the Blazor frontend use `_ = PetApi.TrackInteractionAsync(...)` (fire-and-forget). This ensures the UI is never blocked by tracking calls. Failed tracking calls are silently swallowed — metrics are non-critical.
