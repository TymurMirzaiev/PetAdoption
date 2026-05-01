# Adoption Process Flow Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an adoption request workflow where users express adoption interest, organizations receive and manage requests, and approved users get connected with the organization.

**Architecture:** New AdoptionRequest entity in PetService tracks the adoption lifecycle (Pending → Approved/Rejected/Cancelled). A new AdoptionRequestsController exposes CRUD endpoints. Org admins/moderators review requests via a management page. On approval, the pet transitions to Reserved and the user sees org contact info. Domain events track state changes for future notifications.

**Tech Stack:** .NET 9.0 (PetService), EF Core + SQL Server, custom mediator, Blazor WASM + MudBlazor 8.x, xUnit + FluentAssertions + Testcontainers

**Dependencies:** Plan 4 (Organization Management) must be completed first — this plan references OrganizationId on Pet and org membership concepts.

---

## File Structure

### New files:
- `src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequest.cs` — AdoptionRequest entity
- `src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequestStatus.cs` — Status enum
- `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IAdoptionRequestRepository.cs` — Write repository
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IAdoptionRequestQueryStore.cs` — Read store
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommand.cs` — Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommandHandler.cs` — Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommand.cs` — Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommandHandler.cs` — Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommand.cs` — Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommandHandler.cs` — Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommand.cs` — Command record
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommandHandler.cs` — Handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetAdoptionRequestsByPetQuery.cs` — Query + handler (for org view)
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetMyAdoptionRequestsQuery.cs` — Query + handler (for user view)
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgAdoptionRequestsQuery.cs` — Query + handler (for org dashboard)
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestRepository.cs` — EF Core repository
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestQueryStore.cs` — EF Core query store
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/AdoptionRequestsController.cs` — API endpoints
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/MyAdoptionRequests.razor` — User's adoption requests page
- `src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/OrgAdoptionRequests.razor` — Org adoption request management page
- `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AdoptionRequestTests.cs` — Domain unit tests
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/AdoptionRequestsControllerTests.cs` — Integration tests

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` — Add adoption request error codes
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` — Add AdoptionRequests DbSet + entity config
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` — Map new error codes
- `src/Services/PetService/PetAdoption.PetService.API/Program.cs` — Register repositories and query store
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` — Add adoption request API methods
- `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` — Add adoption request DTOs
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor` — Add "Start Adoption" button
- `src/Web/PetAdoption.Web.BlazorApp/Pages/PetDetail.razor` — Add "Start Adoption" button (if exists)

---

## Chunk 1: Domain Layer — AdoptionRequest Entity + Repository Interface

### Task 1.1: Create AdoptionRequestStatus enum

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequestStatus.cs`

- [ ] **Step 1: Create the enum file**

```csharp
namespace PetAdoption.PetService.Domain;

public enum AdoptionRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequestStatus.cs
git commit -m "feat: add AdoptionRequestStatus enum"
```

### Task 1.2: Create AdoptionRequest entity

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequest.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AdoptionRequestTests.cs`:

```csharp
using FluentAssertions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.UnitTests.Domain;

public class AdoptionRequestTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreatePendingRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act
        var request = AdoptionRequest.Create(userId, petId, organizationId, "I love dogs and have a big yard.");

        // Assert
        request.Id.Should().NotBeEmpty();
        request.UserId.Should().Be(userId);
        request.PetId.Should().Be(petId);
        request.OrganizationId.Should().Be(organizationId);
        request.Status.Should().Be(AdoptionRequestStatus.Pending);
        request.Message.Should().Be("I love dogs and have a big yard.");
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.ReviewedAt.Should().BeNull();
        request.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("petId");
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("organizationId");
    }

    [Fact]
    public void Create_WithNullMessage_ShouldSucceed()
    {
        // Act
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        request.Message.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~AdoptionRequestTests" -v n`
Expected: FAIL — `AdoptionRequest` class does not exist.

- [ ] **Step 3: Write the AdoptionRequest entity**

Create `src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequest.cs`:

```csharp
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain;

public class AdoptionRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public AdoptionRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private AdoptionRequest() { }

    public static AdoptionRequest Create(Guid userId, Guid petId, Guid organizationId, string? message = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId cannot be empty.", nameof(petId));
        if (organizationId == Guid.Empty) throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

        return new AdoptionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PetId = petId,
            OrganizationId = organizationId,
            Status = AdoptionRequestStatus.Pending,
            Message = message?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve()
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be approved because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        Status = AdoptionRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be rejected because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidRejectionReason,
                "Rejection reason is required.",
                new Dictionary<string, object> { { "RequestId", Id } });
        }

        Status = AdoptionRequestStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be cancelled because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        Status = AdoptionRequestStatus.Cancelled;
        ReviewedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~AdoptionRequestTests.Create" -v n`
Expected: PASS (5 tests)

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/AdoptionRequest.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/AdoptionRequestTests.cs
git commit -m "feat: add AdoptionRequest entity with Create factory method and tests"
```

### Task 1.3: Add state transition tests

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AdoptionRequestTests.cs`

- [ ] **Step 1: Add Approve, Reject, Cancel tests**

Append to `AdoptionRequestTests.cs`:

```csharp
    // ──────────────────────────────────────────────────────────────
    // Approve
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_WhenPending_ShouldChangeStatusToApproved()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Approve();

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Approved);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(AdoptionRequestStatus.Approved)]
    [InlineData(AdoptionRequestStatus.Rejected)]
    [InlineData(AdoptionRequestStatus.Cancelled)]
    public void Approve_WhenNotPending_ShouldThrow(AdoptionRequestStatus initialStatus)
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        TransitionTo(request, initialStatus);

        // Act & Assert
        var act = () => request.Approve();
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Reject
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_WhenPending_ShouldChangeStatusToRejected()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Reject("Pet already promised to another family.");

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Rejected);
        request.RejectionReason.Should().Be("Pet already promised to another family.");
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Reject_WithEmptyReason_ShouldThrow(string? reason)
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var act = () => request.Reject(reason!);
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Cancel
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Cancel();

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Cancelled);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Cancel_WhenApproved_ShouldThrow()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Approve();

        // Act & Assert
        var act = () => request.Cancel();
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static void TransitionTo(AdoptionRequest request, AdoptionRequestStatus status)
    {
        switch (status)
        {
            case AdoptionRequestStatus.Approved:
                request.Approve();
                break;
            case AdoptionRequestStatus.Rejected:
                request.Reject("Test rejection reason");
                break;
            case AdoptionRequestStatus.Cancelled:
                request.Cancel();
                break;
        }
    }
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~AdoptionRequestTests" -v n`
Expected: PASS (all tests)

- [ ] **Step 3: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.UnitTests/Domain/AdoptionRequestTests.cs
git commit -m "feat: add AdoptionRequest state transition tests"
```

### Task 1.4: Add domain error codes for adoption requests

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs`

- [ ] **Step 1: Read the current error codes file**

Read `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` to see current values.

- [ ] **Step 2: Add new error codes**

Add these members to the `PetDomainErrorCode` enum:

```csharp
    AdoptionRequestNotFound,
    AdoptionRequestNotPending,
    AdoptionRequestAlreadyExists,
    InvalidRejectionReason
```

- [ ] **Step 3: Map error codes in ExceptionHandlingMiddleware**

Read `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` and add mappings:

- `AdoptionRequestNotFound` → 404
- `AdoptionRequestNotPending` → 409
- `AdoptionRequestAlreadyExists` → 409
- `InvalidRejectionReason` → 400

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs
git commit -m "feat: add adoption request domain error codes and HTTP mappings"
```

### Task 1.5: Create IAdoptionRequestRepository interface

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IAdoptionRequestRepository.cs`

- [ ] **Step 1: Create the repository interface**

```csharp
namespace PetAdoption.PetService.Domain.Interfaces;

public interface IAdoptionRequestRepository
{
    Task<AdoptionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdoptionRequest?> GetPendingByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default);
    Task AddAsync(AdoptionRequest request, CancellationToken ct = default);
    Task UpdateAsync(AdoptionRequest request, CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IAdoptionRequestRepository.cs
git commit -m "feat: add IAdoptionRequestRepository interface"
```

### Task 1.6: Create IAdoptionRequestQueryStore interface

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IAdoptionRequestQueryStore.cs`

- [ ] **Step 1: Create the query store interface**

```csharp
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

public interface IAdoptionRequestQueryStore
{
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take);
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByOrganizationAsync(Guid organizationId, AdoptionRequestStatus? status, int skip, int take);
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByPetAsync(Guid petId, int skip, int take);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/IAdoptionRequestQueryStore.cs
git commit -m "feat: add IAdoptionRequestQueryStore interface"
```

---

## Chunk 2: Infrastructure Layer — EF Core Configuration + Repository

### Task 2.1: Add AdoptionRequests DbSet and entity configuration

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs`

- [ ] **Step 1: Read the current DbContext**

Read `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs`.

- [ ] **Step 2: Add DbSet and entity configuration**

Add to the DbContext class:

```csharp
public DbSet<AdoptionRequest> AdoptionRequests { get; set; }
```

Add entity configuration in `OnModelCreating` (or in a separate `IEntityTypeConfiguration<AdoptionRequest>`):

```csharp
modelBuilder.Entity<AdoptionRequest>(entity =>
{
    entity.ToTable("AdoptionRequests");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.UserId).IsRequired();
    entity.Property(e => e.PetId).IsRequired();
    entity.Property(e => e.OrganizationId).IsRequired();
    entity.Property(e => e.Status)
        .IsRequired()
        .HasConversion<string>();
    entity.Property(e => e.Message).HasMaxLength(2000);
    entity.Property(e => e.RejectionReason).HasMaxLength(2000);
    entity.Property(e => e.CreatedAt).IsRequired();

    entity.HasIndex(e => new { e.UserId, e.PetId, e.Status })
        .HasFilter("[Status] = 'Pending'")
        .IsUnique();

    entity.HasIndex(e => e.OrganizationId);
    entity.HasIndex(e => e.PetId);
});
```

The filtered unique index on `(UserId, PetId) WHERE Status = 'Pending'` prevents duplicate pending requests for the same user+pet combination.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs
git commit -m "feat: add AdoptionRequests DbSet and entity configuration"
```

### Task 2.2: Implement AdoptionRequestRepository

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestRepository.cs`

- [ ] **Step 1: Create the repository implementation**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AdoptionRequestRepository : IAdoptionRequestRepository
{
    private readonly PetServiceDbContext _context;

    public AdoptionRequestRepository(PetServiceDbContext context)
    {
        _context = context;
    }

    public async Task<AdoptionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AdoptionRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<AdoptionRequest?> GetPendingByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default)
    {
        return await _context.AdoptionRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PetId == petId && r.Status == AdoptionRequestStatus.Pending, ct);
    }

    public async Task AddAsync(AdoptionRequest request, CancellationToken ct = default)
    {
        await _context.AdoptionRequests.AddAsync(request, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AdoptionRequest request, CancellationToken ct = default)
    {
        _context.AdoptionRequests.Update(request);
        await _context.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestRepository.cs
git commit -m "feat: implement AdoptionRequestRepository with EF Core"
```

### Task 2.3: Implement AdoptionRequestQueryStore

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestQueryStore.cs`

- [ ] **Step 1: Create the query store implementation**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AdoptionRequestQueryStore : IAdoptionRequestQueryStore
{
    private readonly PetServiceDbContext _context;

    public AdoptionRequestQueryStore(PetServiceDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take)
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.LongCountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByOrganizationAsync(
        Guid organizationId, AdoptionRequestStatus? status, int skip, int take)
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.LongCountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByPetAsync(Guid petId, int skip, int take)
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.PetId == petId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.LongCountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AdoptionRequestQueryStore.cs
git commit -m "feat: implement AdoptionRequestQueryStore"
```

### Task 2.4: Register repositories in DI

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Program.cs`

- [ ] **Step 1: Read Program.cs**

Read `src/Services/PetService/PetAdoption.PetService.API/Program.cs` to find where other repositories are registered.

- [ ] **Step 2: Add DI registrations**

Add alongside existing repository registrations:

```csharp
builder.Services.AddScoped<IAdoptionRequestRepository, AdoptionRequestRepository>();
builder.Services.AddScoped<IAdoptionRequestQueryStore, AdoptionRequestQueryStore>();
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Program.cs
git commit -m "feat: register adoption request repositories in DI"
```

---

## Chunk 3: Application Layer — Command and Query Handlers

### Task 3.1: Create adoption request command + handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommandHandler.cs`

- [ ] **Step 1: Create the command record**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAdoptionRequestCommand(
    Guid UserId,
    Guid PetId,
    string? Message) : IRequest<CreateAdoptionRequestResponse>;
```

- [ ] **Step 2: Create the handler with response**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAdoptionRequestResponse(Guid Id, string Status);

public class CreateAdoptionRequestCommandHandler : IRequestHandler<CreateAdoptionRequestCommand, CreateAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IPetQueryStore _petQueryStore;

    public CreateAdoptionRequestCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IPetQueryStore petQueryStore)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _petQueryStore = petQueryStore;
    }

    public async Task<CreateAdoptionRequestResponse> Handle(
        CreateAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _petQueryStore.GetById(request.PetId);
        if (pet is null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });
        }

        if (pet.Status != PetStatus.Available)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotAvailable,
                $"Pet {request.PetId} is not available for adoption.",
                new Dictionary<string, object> { { "PetId", request.PetId }, { "Status", pet.Status.ToString() } });
        }

        var existing = await _adoptionRequestRepository.GetPendingByUserAndPetAsync(
            request.UserId, request.PetId, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestAlreadyExists,
                $"A pending adoption request already exists for this pet.",
                new Dictionary<string, object>
                {
                    { "UserId", request.UserId },
                    { "PetId", request.PetId },
                    { "ExistingRequestId", existing.Id }
                });
        }

        var organizationId = pet.OrganizationId ?? throw new DomainException(
            PetDomainErrorCode.InvalidOperation,
            "Pet is not assigned to an organization.",
            new Dictionary<string, object> { { "PetId", request.PetId } });

        var adoptionRequest = AdoptionRequest.Create(
            request.UserId, request.PetId, organizationId, request.Message);

        await _adoptionRequestRepository.AddAsync(adoptionRequest, cancellationToken);

        return new CreateAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
```

**Note:** This handler reads `pet.OrganizationId` which is added by Plan 4 (Organization Management). If Plan 4 hasn't been implemented yet, this field won't exist and you'll need to implement Plan 4 first.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateAdoptionRequestCommandHandler.cs
git commit -m "feat: add CreateAdoptionRequest command and handler"
```

### Task 3.2: Approve adoption request command + handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommandHandler.cs`

- [ ] **Step 1: Create the command record**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record ApproveAdoptionRequestCommand(Guid RequestId, Guid ReviewerId) : IRequest<ApproveAdoptionRequestResponse>;
```

- [ ] **Step 2: Create the handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record ApproveAdoptionRequestResponse(Guid Id, string Status);

public class ApproveAdoptionRequestCommandHandler : IRequestHandler<ApproveAdoptionRequestCommand, ApproveAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IPetRepository _petRepository;

    public ApproveAdoptionRequestCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IPetRepository petRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _petRepository = petRepository;
    }

    public async Task<ApproveAdoptionRequestResponse> Handle(
        ApproveAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        adoptionRequest.Approve();

        var pet = await _petRepository.GetByIdAsync(adoptionRequest.PetId)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {adoptionRequest.PetId} not found.",
                new Dictionary<string, object> { { "PetId", adoptionRequest.PetId } });

        pet.Reserve();

        await _petRepository.UpdateAsync(pet);
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new ApproveAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommandHandler.cs
git commit -m "feat: add ApproveAdoptionRequest command and handler"
```

### Task 3.3: Reject adoption request command + handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommandHandler.cs`

- [ ] **Step 1: Create the command record**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record RejectAdoptionRequestCommand(Guid RequestId, Guid ReviewerId, string Reason) : IRequest<RejectAdoptionRequestResponse>;
```

- [ ] **Step 2: Create the handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record RejectAdoptionRequestResponse(Guid Id, string Status);

public class RejectAdoptionRequestCommandHandler : IRequestHandler<RejectAdoptionRequestCommand, RejectAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;

    public RejectAdoptionRequestCommandHandler(IAdoptionRequestRepository adoptionRequestRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
    }

    public async Task<RejectAdoptionRequestResponse> Handle(
        RejectAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        adoptionRequest.Reject(request.Reason);
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new RejectAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/RejectAdoptionRequestCommandHandler.cs
git commit -m "feat: add RejectAdoptionRequest command and handler"
```

### Task 3.4: Cancel adoption request command + handler

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommandHandler.cs`

- [ ] **Step 1: Create the command record**

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CancelAdoptionRequestCommand(Guid RequestId, Guid UserId) : IRequest<CancelAdoptionRequestResponse>;
```

- [ ] **Step 2: Create the handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CancelAdoptionRequestResponse(Guid Id, string Status);

public class CancelAdoptionRequestCommandHandler : IRequestHandler<CancelAdoptionRequestCommand, CancelAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;

    public CancelAdoptionRequestCommandHandler(IAdoptionRequestRepository adoptionRequestRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
    }

    public async Task<CancelAdoptionRequestResponse> Handle(
        CancelAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        if (adoptionRequest.UserId != request.UserId)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                "Only the requesting user can cancel their adoption request.",
                new Dictionary<string, object>
                {
                    { "RequestId", request.RequestId },
                    { "RequestUserId", adoptionRequest.UserId },
                    { "CallerUserId", request.UserId }
                });
        }

        adoptionRequest.Cancel();
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new CancelAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/CancelAdoptionRequestCommandHandler.cs
git commit -m "feat: add CancelAdoptionRequest command and handler"
```

### Task 3.5: Query handlers for adoption requests

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetMyAdoptionRequestsQuery.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgAdoptionRequestsQuery.cs`

- [ ] **Step 1: Create GetMyAdoptionRequestsQuery (user's own requests)**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetMyAdoptionRequestsQuery(Guid UserId, int Skip = 0, int Take = 20)
    : IRequest<GetMyAdoptionRequestsResponse>;

public record AdoptionRequestDto(
    Guid Id,
    Guid PetId,
    string PetName,
    string PetType,
    Guid OrganizationId,
    string Status,
    string? Message,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record GetMyAdoptionRequestsResponse(
    List<AdoptionRequestDto> Items,
    long Total,
    int Skip,
    int Take);

public class GetMyAdoptionRequestsQueryHandler
    : IRequestHandler<GetMyAdoptionRequestsQuery, GetMyAdoptionRequestsResponse>
{
    private readonly IAdoptionRequestQueryStore _queryStore;
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetMyAdoptionRequestsQueryHandler(
        IAdoptionRequestQueryStore queryStore,
        IPetQueryStore petQueryStore,
        IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petQueryStore = petQueryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetMyAdoptionRequestsResponse> Handle(
        GetMyAdoptionRequestsQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetByUserAsync(request.UserId, request.Skip, request.Take);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var dtos = new List<AdoptionRequestDto>();
        foreach (var item in items)
        {
            var pet = await _petQueryStore.GetById(item.PetId);
            var petTypeName = pet is not null && petTypeDict.TryGetValue(pet.PetTypeId, out var name) ? name : "Unknown";

            dtos.Add(new AdoptionRequestDto(
                item.Id,
                item.PetId,
                pet?.Name?.Value ?? "Unknown",
                petTypeName,
                item.OrganizationId,
                item.Status.ToString(),
                item.Message,
                item.RejectionReason,
                item.CreatedAt,
                item.ReviewedAt));
        }

        return new GetMyAdoptionRequestsResponse(dtos, total, request.Skip, request.Take);
    }
}
```

- [ ] **Step 2: Create GetOrgAdoptionRequestsQuery (org dashboard)**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgAdoptionRequestsQuery(
    Guid OrganizationId,
    AdoptionRequestStatus? Status = null,
    int Skip = 0,
    int Take = 20) : IRequest<GetOrgAdoptionRequestsResponse>;

public record OrgAdoptionRequestDto(
    Guid Id,
    Guid UserId,
    Guid PetId,
    string PetName,
    string Status,
    string? Message,
    DateTime CreatedAt);

public record GetOrgAdoptionRequestsResponse(
    List<OrgAdoptionRequestDto> Items,
    long Total,
    int Skip,
    int Take);

public class GetOrgAdoptionRequestsQueryHandler
    : IRequestHandler<GetOrgAdoptionRequestsQuery, GetOrgAdoptionRequestsResponse>
{
    private readonly IAdoptionRequestQueryStore _queryStore;
    private readonly IPetQueryStore _petQueryStore;

    public GetOrgAdoptionRequestsQueryHandler(
        IAdoptionRequestQueryStore queryStore,
        IPetQueryStore petQueryStore)
    {
        _queryStore = queryStore;
        _petQueryStore = petQueryStore;
    }

    public async Task<GetOrgAdoptionRequestsResponse> Handle(
        GetOrgAdoptionRequestsQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetByOrganizationAsync(
            request.OrganizationId, request.Status, request.Skip, request.Take);

        var dtos = new List<OrgAdoptionRequestDto>();
        foreach (var item in items)
        {
            var pet = await _petQueryStore.GetById(item.PetId);
            dtos.Add(new OrgAdoptionRequestDto(
                item.Id,
                item.UserId,
                item.PetId,
                pet?.Name?.Value ?? "Unknown",
                item.Status.ToString(),
                item.Message,
                item.CreatedAt));
        }

        return new GetOrgAdoptionRequestsResponse(dtos, total, request.Skip, request.Take);
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/GetMyAdoptionRequestsQuery.cs src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgAdoptionRequestsQuery.cs
git commit -m "feat: add adoption request query handlers"
```

---

## Chunk 4: API Layer — AdoptionRequestsController

### Task 4.1: Create AdoptionRequestsController

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.API/Controllers/AdoptionRequestsController.cs`

- [ ] **Step 1: Create the controller**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/adoption-requests")]
[Authorize]
public class AdoptionRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdoptionRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Create an adoption request for a pet. User must be authenticated.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdoptionRequest([FromBody] CreateAdoptionRequestBody body)
    {
        var result = await _mediator.Send(new CreateAdoptionRequestCommand(
            GetUserId(), body.PetId, body.Message));
        return StatusCode(201, result);
    }

    /// <summary>
    /// Get the current user's adoption requests.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyAdoptionRequests(
        [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _mediator.Send(new GetMyAdoptionRequestsQuery(GetUserId(), skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Get adoption requests for an organization. Caller must be org admin/moderator.
    /// Organization membership is validated by the caller (Blazor frontend checks via UserService).
    /// </summary>
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetOrgAdoptionRequests(
        Guid organizationId,
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        AdoptionRequestStatus? statusFilter = status is not null
            ? Enum.Parse<AdoptionRequestStatus>(status, ignoreCase: true)
            : null;

        var result = await _mediator.Send(new GetOrgAdoptionRequestsQuery(
            organizationId, statusFilter, skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Approve an adoption request. Caller must be org admin/moderator.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveAdoptionRequest(Guid id)
    {
        var result = await _mediator.Send(new ApproveAdoptionRequestCommand(id, GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Reject an adoption request. Caller must be org admin/moderator.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectAdoptionRequest(Guid id, [FromBody] RejectAdoptionRequestBody body)
    {
        var result = await _mediator.Send(new RejectAdoptionRequestCommand(id, GetUserId(), body.Reason));
        return Ok(result);
    }

    /// <summary>
    /// Cancel an adoption request. Only the requesting user can cancel.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelAdoptionRequest(Guid id)
    {
        var result = await _mediator.Send(new CancelAdoptionRequestCommand(id, GetUserId()));
        return Ok(result);
    }
}

public record CreateAdoptionRequestBody(Guid PetId, string? Message);
public record RejectAdoptionRequestBody(string Reason);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/AdoptionRequestsController.cs
git commit -m "feat: add AdoptionRequestsController with CRUD endpoints"
```

---

## Chunk 5: Integration Tests

### Task 5.1: Create integration tests for adoption request endpoints

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/AdoptionRequestsControllerTests.cs`

- [ ] **Step 1: Read the existing integration test setup**

Read `tests/PetService/PetAdoption.PetService.IntegrationTests/` to understand the test factory, base class, and helper patterns used.

- [ ] **Step 2: Write the integration tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class AdoptionRequestsControllerTests : IAsyncLifetime
{
    private readonly PetServiceWebAppFactory _factory;
    private HttpClient _userClient = null!;
    private HttpClient _orgAdminClient = null!;

    public AdoptionRequestsControllerTests(SqlServerFixture fixture)
    {
        _factory = new PetServiceWebAppFactory(fixture, nameof(AdoptionRequestsControllerTests));
    }

    public async Task InitializeAsync()
    {
        _userClient = _factory.CreateAuthenticatedClient(role: "User");
        _orgAdminClient = _factory.CreateAuthenticatedClient(role: "Admin");
        await SeedTestPet();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private Guid _testPetId;
    private Guid _testOrgId = Guid.NewGuid();

    private async Task SeedTestPet()
    {
        // Create a pet type first, then create a pet assigned to an org
        // Adjust this based on how PetServiceWebAppFactory and seed data work
        // The pet must have an OrganizationId (Plan 4 dependency)
        var createPetResponse = await _orgAdminClient.PostAsJsonAsync("api/pets", new
        {
            Name = "Buddy",
            PetTypeId = Guid.NewGuid(), // Use a seeded pet type ID
            Breed = "Golden Retriever",
            AgeMonths = 24,
            Description = "Friendly dog"
        });

        if (createPetResponse.IsSuccessStatusCode)
        {
            var pet = await createPetResponse.Content.ReadFromJsonAsync<CreatePetResult>();
            _testPetId = pet!.Id;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Create Adoption Request
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAdoptionRequest_WithValidData_Returns201()
    {
        // Arrange
        var body = new { PetId = _testPetId, Message = "I'd love to adopt Buddy!" };

        // Act
        var response = await _userClient.PostAsJsonAsync("api/adoption-requests", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AdoptionRequestResult>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateAdoptionRequest_DuplicatePending_Returns409()
    {
        // Arrange
        var body = new { PetId = _testPetId, Message = "First request" };
        await _userClient.PostAsJsonAsync("api/adoption-requests", body);

        // Act
        var response = await _userClient.PostAsJsonAsync("api/adoption-requests", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────
    // Get My Adoption Requests
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAdoptionRequests_ReturnsUserRequests()
    {
        // Arrange
        await _userClient.PostAsJsonAsync("api/adoption-requests",
            new { PetId = _testPetId, Message = "Please!" });

        // Act
        var response = await _userClient.GetFromJsonAsync<AdoptionRequestListResult>("api/adoption-requests/mine");

        // Assert
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Approve / Reject / Cancel
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAdoptionRequest_WhenPending_Returns200()
    {
        // Arrange
        var createResponse = await _userClient.PostAsJsonAsync("api/adoption-requests",
            new { PetId = _testPetId, Message = "Please approve!" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResult>();

        // Act
        var response = await _orgAdminClient.PostAsync($"api/adoption-requests/{created!.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AdoptionRequestResult>();
        result!.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task CancelAdoptionRequest_ByOwner_Returns200()
    {
        // Arrange
        var createResponse = await _userClient.PostAsJsonAsync("api/adoption-requests",
            new { PetId = _testPetId, Message = "Changed my mind" });
        var created = await createResponse.Content.ReadFromJsonAsync<AdoptionRequestResult>();

        // Act
        var response = await _userClient.PostAsync($"api/adoption-requests/{created!.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AdoptionRequestResult>();
        result!.Status.Should().Be("Cancelled");
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResult(Guid Id);
    private record AdoptionRequestResult(Guid Id, string Status);
    private record AdoptionRequestListResult(
        List<AdoptionRequestItemResult> Items, long Total, int Skip, int Take);
    private record AdoptionRequestItemResult(
        Guid Id, Guid PetId, string PetName, string Status, DateTime CreatedAt);
}
```

**Note:** These tests are scaffolds. You will need to adapt them based on the actual test infrastructure (the `PetServiceWebAppFactory` helper methods, how tokens are generated, and how seed pet types work). Read the existing integration test files first.

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~AdoptionRequestsControllerTests" -v n`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/AdoptionRequestsControllerTests.cs
git commit -m "feat: add adoption request integration tests"
```

---

## Chunk 6: Blazor Frontend — API Client + Models + Pages

### Task 6.1: Add adoption request DTOs to ApiModels.cs

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs`

- [ ] **Step 1: Read the current file**

Read `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs`.

- [ ] **Step 2: Add adoption request models**

Append to the file:

```csharp
public record AdoptionRequestItem(
    Guid Id, Guid PetId, string PetName, string PetType, Guid OrganizationId,
    string Status, string? Message, string? RejectionReason,
    DateTime CreatedAt, DateTime? ReviewedAt);
public record AdoptionRequestsResponse(
    IEnumerable<AdoptionRequestItem> Items, long Total, int Skip, int Take);
public record OrgAdoptionRequestItem(
    Guid Id, Guid UserId, Guid PetId, string PetName,
    string Status, string? Message, DateTime CreatedAt);
public record OrgAdoptionRequestsResponse(
    IEnumerable<OrgAdoptionRequestItem> Items, long Total, int Skip, int Take);
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs
git commit -m "feat: add adoption request DTOs to ApiModels"
```

### Task 6.2: Add adoption request methods to PetApiClient

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`

- [ ] **Step 1: Read the current file**

Read `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`.

- [ ] **Step 2: Add adoption request API methods**

Append to the `PetApiClient` class:

```csharp
    public Task<HttpResponseMessage> CreateAdoptionRequestAsync(Guid petId, string? message = null) =>
        _http.PostAsJsonAsync("api/adoption-requests", new { PetId = petId, Message = message });

    public async Task<AdoptionRequestsResponse?> GetMyAdoptionRequestsAsync(int skip = 0, int take = 20) =>
        await _http.GetFromJsonAsync<AdoptionRequestsResponse>($"api/adoption-requests/mine?skip={skip}&take={take}");

    public async Task<OrgAdoptionRequestsResponse?> GetOrgAdoptionRequestsAsync(
        Guid organizationId, string? status = null, int skip = 0, int take = 20)
    {
        var url = $"api/adoption-requests/organization/{organizationId}?skip={skip}&take={take}";
        if (status is not null) url += $"&status={status}";
        return await _http.GetFromJsonAsync<OrgAdoptionRequestsResponse>(url);
    }

    public Task<HttpResponseMessage> ApproveAdoptionRequestAsync(Guid requestId) =>
        _http.PostAsync($"api/adoption-requests/{requestId}/approve", null);

    public Task<HttpResponseMessage> RejectAdoptionRequestAsync(Guid requestId, string reason) =>
        _http.PostAsJsonAsync($"api/adoption-requests/{requestId}/reject", new { Reason = reason });

    public Task<HttpResponseMessage> CancelAdoptionRequestAsync(Guid requestId) =>
        _http.PostAsync($"api/adoption-requests/{requestId}/cancel", null);
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs
git commit -m "feat: add adoption request methods to PetApiClient"
```

### Task 6.3: Add "Start Adoption" button to Favorites page

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor`

- [ ] **Step 1: Read the current Favorites.razor**

Read `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor`.

- [ ] **Step 2: Add Start Adoption button and dialog**

In the `<MudCardActions>` section, add a "Start Adoption" button next to the existing "Reserve" button (for Available pets only):

```razor
@if (fav.Status == "Available")
{
    <MudButton Size="Size.Small" Color="Color.Success"
        OnClick="@(() => ReservePet(fav.PetId))">Reserve</MudButton>
    <MudButton Size="Size.Small" Color="Color.Tertiary"
        OnClick="@(() => OpenAdoptionDialog(fav.PetId, fav.PetName))">Adopt</MudButton>
}
```

Add dialog and handler in the `@code` block:

```csharp
    private bool _adoptDialogVisible;
    private string _adoptMessage = "";
    private Guid _adoptPetId;
    private string _adoptPetName = "";

    private void OpenAdoptionDialog(Guid petId, string petName)
    {
        _adoptPetId = petId;
        _adoptPetName = petName;
        _adoptMessage = "";
        _adoptDialogVisible = true;
    }

    private async Task SubmitAdoptionRequest()
    {
        try
        {
            var response = await PetApi.CreateAdoptionRequestAsync(_adoptPetId, _adoptMessage);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add($"Adoption request sent for {_adoptPetName}!", Severity.Success);
                _adoptDialogVisible = false;
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                Snackbar.Add(error?.Message ?? "Failed to send adoption request", Severity.Error);
            }
        }
        catch { Snackbar.Add("Connection error", Severity.Error); }
    }
```

Add the dialog markup before the closing `</MudContainer>`:

```razor
<MudDialog @bind-Visible="_adoptDialogVisible" Options="@(new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true })">
    <TitleContent>
        <MudText Typo="Typo.h6">Adopt @_adoptPetName</MudText>
    </TitleContent>
    <DialogContent>
        <MudText Typo="Typo.body1" Class="mb-4">
            Tell the organization why you'd like to adopt this pet (optional):
        </MudText>
        <MudTextField @bind-Value="_adoptMessage" Label="Your message" Lines="3" Variant="Variant.Outlined" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => _adoptDialogVisible = false)">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="SubmitAdoptionRequest">Send Request</MudButton>
    </DialogActions>
</MudDialog>
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor
git commit -m "feat: add Start Adoption button and dialog to Favorites page"
```

### Task 6.4: Create MyAdoptionRequests page

**Files:**
- Create: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/MyAdoptionRequests.razor`

- [ ] **Step 1: Create the page**

```razor
@page "/my-adoption-requests"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject ISnackbar Snackbar

<PageTitle>My Adoption Requests</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">My Adoption Requests</MudText>

    @if (_loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (!_requests.Any())
    {
        <MudText Typo="Typo.h6" Color="Color.Secondary" Align="Align.Center" Class="mt-8">
            No adoption requests yet. Browse pets and start the adoption process!
        </MudText>
        <div class="d-flex justify-center mt-4">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/favorites">My Favorites</MudButton>
        </div>
    }
    else
    {
        <MudTable Items="_requests" Hover="true" Elevation="3">
            <HeaderContent>
                <MudTh>Pet</MudTh>
                <MudTh>Type</MudTh>
                <MudTh>Status</MudTh>
                <MudTh>Submitted</MudTh>
                <MudTh>Reviewed</MudTh>
                <MudTh>Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.PetName</MudTd>
                <MudTd>@context.PetType</MudTd>
                <MudTd>
                    <MudChip T="string" Size="Size.Small" Color="@StatusColor(context.Status)">@context.Status</MudChip>
                </MudTd>
                <MudTd>@context.CreatedAt.ToLocalTime().ToString("g")</MudTd>
                <MudTd>@(context.ReviewedAt?.ToLocalTime().ToString("g") ?? "-")</MudTd>
                <MudTd>
                    @if (context.Status == "Pending")
                    {
                        <MudButton Size="Size.Small" Color="Color.Error" Variant="Variant.Text"
                            OnClick="@(() => CancelRequest(context.Id))">Cancel</MudButton>
                    }
                    @if (context.Status == "Rejected" && context.RejectionReason is not null)
                    {
                        <MudTooltip Text="@context.RejectionReason">
                            <MudIconButton Icon="@Icons.Material.Filled.Info" Size="Size.Small" Color="Color.Warning" />
                        </MudTooltip>
                    }
                </MudTd>
            </RowTemplate>
        </MudTable>
    }
</MudContainer>

@code {
    private List<AdoptionRequestItem> _requests = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync() => await LoadRequests();

    private async Task LoadRequests()
    {
        _loading = true;
        try
        {
            var response = await PetApi.GetMyAdoptionRequestsAsync(take: 50);
            _requests = response?.Items?.ToList() ?? [];
        }
        catch { Snackbar.Add("Failed to load adoption requests", Severity.Error); }
        finally { _loading = false; }
    }

    private async Task CancelRequest(Guid requestId)
    {
        try
        {
            var response = await PetApi.CancelAdoptionRequestAsync(requestId);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Request cancelled", Severity.Info);
                await LoadRequests();
            }
        }
        catch { Snackbar.Add("Failed to cancel request", Severity.Error); }
    }

    private static Color StatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Approved" => Color.Success,
        "Rejected" => Color.Error,
        "Cancelled" => Color.Default,
        _ => Color.Default
    };
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/MyAdoptionRequests.razor
git commit -m "feat: add MyAdoptionRequests page"
```

### Task 6.5: Create OrgAdoptionRequests page

**Files:**
- Create: `src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/OrgAdoptionRequests.razor`

- [ ] **Step 1: Create the page**

```razor
@page "/organization/{OrganizationId:guid}/adoption-requests"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject ISnackbar Snackbar

<PageTitle>Adoption Requests</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Adoption Requests</MudText>

    <MudSelect T="string" Label="Filter by Status" @bind-Value="_statusFilter" @bind-Value:after="LoadRequests" Class="mb-4" Clearable="true">
        <MudSelectItem Value="@("Pending")">Pending</MudSelectItem>
        <MudSelectItem Value="@("Approved")">Approved</MudSelectItem>
        <MudSelectItem Value="@("Rejected")">Rejected</MudSelectItem>
        <MudSelectItem Value="@("Cancelled")">Cancelled</MudSelectItem>
    </MudSelect>

    @if (_loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (!_requests.Any())
    {
        <MudText Typo="Typo.h6" Color="Color.Secondary" Align="Align.Center" Class="mt-8">
            No adoption requests found.
        </MudText>
    }
    else
    {
        <MudTable Items="_requests" Hover="true" Elevation="3">
            <HeaderContent>
                <MudTh>Pet</MudTh>
                <MudTh>Status</MudTh>
                <MudTh>Message</MudTh>
                <MudTh>Submitted</MudTh>
                <MudTh>Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.PetName</MudTd>
                <MudTd>
                    <MudChip T="string" Size="Size.Small" Color="@StatusColor(context.Status)">@context.Status</MudChip>
                </MudTd>
                <MudTd>@(context.Message ?? "-")</MudTd>
                <MudTd>@context.CreatedAt.ToLocalTime().ToString("g")</MudTd>
                <MudTd>
                    @if (context.Status == "Pending")
                    {
                        <MudButton Size="Size.Small" Color="Color.Success" Variant="Variant.Filled"
                            OnClick="@(() => ApproveRequest(context.Id))">Approve</MudButton>
                        <MudButton Size="Size.Small" Color="Color.Error" Variant="Variant.Text"
                            OnClick="@(() => OpenRejectDialog(context.Id, context.PetName))">Reject</MudButton>
                    }
                </MudTd>
            </RowTemplate>
        </MudTable>
    }

    <MudDialog @bind-Visible="_rejectDialogVisible" Options="@(new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true })">
        <TitleContent>
            <MudText Typo="Typo.h6">Reject Request for @_rejectPetName</MudText>
        </TitleContent>
        <DialogContent>
            <MudTextField @bind-Value="_rejectReason" Label="Rejection reason" Lines="3" Variant="Variant.Outlined" Required="true" />
        </DialogContent>
        <DialogActions>
            <MudButton OnClick="@(() => _rejectDialogVisible = false)">Cancel</MudButton>
            <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="SubmitReject"
                Disabled="@string.IsNullOrWhiteSpace(_rejectReason)">Reject</MudButton>
        </DialogActions>
    </MudDialog>
</MudContainer>

@code {
    [Parameter] public Guid OrganizationId { get; set; }

    private List<OrgAdoptionRequestItem> _requests = [];
    private bool _loading = true;
    private string? _statusFilter;

    private bool _rejectDialogVisible;
    private Guid _rejectRequestId;
    private string _rejectPetName = "";
    private string _rejectReason = "";

    protected override async Task OnInitializedAsync() => await LoadRequests();

    private async Task LoadRequests()
    {
        _loading = true;
        try
        {
            var response = await PetApi.GetOrgAdoptionRequestsAsync(OrganizationId, _statusFilter, take: 50);
            _requests = response?.Items?.ToList() ?? [];
        }
        catch { Snackbar.Add("Failed to load adoption requests", Severity.Error); }
        finally { _loading = false; }
    }

    private async Task ApproveRequest(Guid requestId)
    {
        try
        {
            var response = await PetApi.ApproveAdoptionRequestAsync(requestId);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Request approved! Pet has been reserved.", Severity.Success);
                await LoadRequests();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                Snackbar.Add(error?.Message ?? "Failed to approve", Severity.Error);
            }
        }
        catch { Snackbar.Add("Connection error", Severity.Error); }
    }

    private void OpenRejectDialog(Guid requestId, string petName)
    {
        _rejectRequestId = requestId;
        _rejectPetName = petName;
        _rejectReason = "";
        _rejectDialogVisible = true;
    }

    private async Task SubmitReject()
    {
        try
        {
            var response = await PetApi.RejectAdoptionRequestAsync(_rejectRequestId, _rejectReason);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Request rejected", Severity.Info);
                _rejectDialogVisible = false;
                await LoadRequests();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                Snackbar.Add(error?.Message ?? "Failed to reject", Severity.Error);
            }
        }
        catch { Snackbar.Add("Connection error", Severity.Error); }
    }

    private static Color StatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Approved" => Color.Success,
        "Rejected" => Color.Error,
        "Cancelled" => Color.Default,
        _ => Color.Default
    };
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/OrgAdoptionRequests.razor
git commit -m "feat: add OrgAdoptionRequests management page"
```

### Task 6.6: Add navigation link for My Adoption Requests

**Files:**
- Modify: Wherever the navigation menu is defined (likely `Shared/NavMenu.razor` or `MainLayout.razor`)

- [ ] **Step 1: Read the navigation component**

Read the navigation menu file to find where nav links are defined.

- [ ] **Step 2: Add navigation link**

Add a link to "My Adoption Requests":

```razor
<MudNavLink Href="/my-adoption-requests" Icon="@Icons.Material.Filled.Pets">My Requests</MudNavLink>
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/
git commit -m "feat: add navigation link for My Adoption Requests"
```
