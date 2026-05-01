# Organization Management + Platform Admin Implementation Plan [COMPLETED]

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add multi-tenant organization support with platform-level admin role, enabling organization CRUD, membership management, and org-scoped pet ownership.

**Architecture:** Organization and OrganizationMember entities live in UserService (user domain). A new PlatformAdmin role enables platform-level management. Pet aggregate in PetService gains a nullable OrganizationId for org-scoped ownership. Two new controllers in UserService handle org and member CRUD. PetService's AdminOnly policy is updated to also accept PlatformAdmin.

**Tech Stack:** .NET 10.0 (UserService), .NET 9.0 (PetService), EF Core + SQL Server, xUnit + FluentAssertions + Moq + Testcontainers

---

## File Structure

### New files:
- `src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrganizationStatus.cs`
- `src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrgRole.cs`
- `src/Services/UserService/PetAdoption.UserService.Domain/Entities/Organization.cs`
- `src/Services/UserService/PetAdoption.UserService.Domain/Entities/OrganizationMember.cs`
- `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationRepository.cs`
- `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationMemberRepository.cs`
- `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationRepository.cs`
- `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationMemberRepository.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/CreateOrganizationCommand.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/CreateOrganizationCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/UpdateOrganizationCommand.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/UpdateOrganizationCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/DeactivateOrganizationCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/ActivateOrganizationCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/AddOrganizationMemberCommand.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/AddOrganizationMemberCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/RemoveOrganizationMemberCommandHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/GetOrganizationsQueryHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/GetOrganizationByIdQueryHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/GetOrganizationMembersQueryHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/GetMyOrganizationsQueryHandler.cs`
- `src/Services/UserService/PetAdoption.UserService.API/Controllers/OrganizationsController.cs`
- `tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationTests.cs`
- `tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationMemberTests.cs`
- `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/OrganizationManagementTests.cs`
- `tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/CreateOrganizationRequestBuilder.cs`

### Modified files:
- `src/Services/UserService/PetAdoption.UserService.Domain/Enums/UserRole.cs` -- add PlatformAdmin = 2
- `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/UserServiceDbContext.cs` -- add DbSets + entity configs
- `src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` -- register repos + handlers
- `src/Services/UserService/PetAdoption.UserService.API/Program.cs` -- add PlatformAdminOnly policy
- `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs` -- add OrganizationId + AssignToOrganization()
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` -- add OrganizationId column
- `src/Services/PetService/PetAdoption.PetService.API/Program.cs` -- update AdminOnly policy to include PlatformAdmin

---

## Chunk 1: UserService Domain Layer

### Task 1: Add PlatformAdmin to UserRole enum

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.Domain/Enums/UserRole.cs`

- [ ] **Step 1: Add PlatformAdmin value**

```csharp
namespace PetAdoption.UserService.Domain.Enums;

public enum UserRole
{
    User = 0,
    Admin = 1,
    PlatformAdmin = 2
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.Domain`

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Enums/UserRole.cs
git commit -m "add PlatformAdmin role to UserRole enum"
```

---

### Task 2: Create OrganizationStatus and OrgRole enums

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrganizationStatus.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrgRole.cs`

- [ ] **Step 1: Create OrganizationStatus enum**

```csharp
namespace PetAdoption.UserService.Domain.Enums;

public enum OrganizationStatus
{
    Active = 0,
    Inactive = 1
}
```

- [ ] **Step 2: Create OrgRole enum**

```csharp
namespace PetAdoption.UserService.Domain.Enums;

public enum OrgRole
{
    Admin = 0,
    Moderator = 1
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrganizationStatus.cs src/Services/UserService/PetAdoption.UserService.Domain/Enums/OrgRole.cs
git commit -m "add OrganizationStatus and OrgRole enums"
```

---

### Task 3: Create Organization entity

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Entities/Organization.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationTests.cs`

- [ ] **Step 1: Write unit tests**

```csharp
using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.UnitTests.Domain;

public class OrganizationTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreateActiveOrganization()
    {
        // Act
        var org = Organization.Create("Happy Paws", "happy-paws", "A shelter");

        // Assert
        org.Id.Should().NotBeEmpty();
        org.Name.Should().Be("Happy Paws");
        org.Slug.Should().Be("happy-paws");
        org.Description.Should().Be("A shelter");
        org.Status.Should().Be(OrganizationStatus.Active);
        org.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithDeterministicId_ShouldUseProvidedId()
    {
        // Arrange
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var org = Organization.Create(id, "Happy Paws", "happy-paws", "A shelter");

        // Assert
        org.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void Create_WithInvalidName_ShouldThrow(string name)
    {
        // Act & Assert
        var act = () => Organization.Create(name, "valid-slug", null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("UPPER")]
    [InlineData("has spaces")]
    [InlineData("special!chars")]
    public void Create_WithInvalidSlug_ShouldThrow(string slug)
    {
        // Act & Assert
        var act = () => Organization.Create("Valid Name", slug, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithValidSlug_ShouldAccept()
    {
        // Act
        var org = Organization.Create("Test", "valid-slug-123", null);

        // Assert
        org.Slug.Should().Be("valid-slug-123");
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var org = Organization.Create("Old Name", "slug", "Old desc");

        // Act
        org.Update("New Name", "New desc");

        // Assert
        org.Name.Should().Be("New Name");
        org.Description.Should().Be("New desc");
        org.UpdatedAt.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // Deactivate / Activate
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_ShouldSetInactive()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);

        // Act
        org.Deactivate();

        // Assert
        org.Status.Should().Be(OrganizationStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrow()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);
        org.Deactivate();

        // Act & Assert
        var act = () => org.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetActive()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);
        org.Deactivate();

        // Act
        org.Activate();

        // Assert
        org.Status.Should().Be(OrganizationStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);

        // Act & Assert
        var act = () => org.Activate();
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "OrganizationTests"`
Expected: Build fails (Organization class doesn't exist yet)

- [ ] **Step 3: Implement Organization entity**

```csharp
using System.Text.RegularExpressions;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.Domain.Entities;

public class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Organization() { }

    public static Organization Create(string name, string slug, string? description)
    {
        return Create(Guid.NewGuid(), name, slug, description);
    }

    public static Organization Create(Guid id, string name, string slug, string? description)
    {
        ValidateName(name);
        ValidateSlug(slug);

        return new Organization
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            Status = OrganizationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        ValidateName(name);
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == OrganizationStatus.Inactive)
            throw new InvalidOperationException("Organization is already inactive");
        Status = OrganizationStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == OrganizationStatus.Active)
            throw new InvalidOperationException("Organization is already active");
        Status = OrganizationStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
            throw new ArgumentException("Organization name must be between 2 and 100 characters.", nameof(name));
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));
        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length < 2 || trimmed.Length > 50)
            throw new ArgumentException("Slug must be between 2 and 50 characters.", nameof(slug));
        if (!SlugPattern.IsMatch(trimmed))
            throw new ArgumentException("Slug must be lowercase alphanumeric with hyphens only.", nameof(slug));
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "OrganizationTests"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Entities/Organization.cs tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationTests.cs
git commit -m "add Organization entity with factory methods, validation, and unit tests"
```

---

### Task 4: Create OrganizationMember entity

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Entities/OrganizationMember.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationMemberTests.cs`

- [ ] **Step 1: Write unit tests**

```csharp
using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.UnitTests.Domain;

public class OrganizationMemberTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreateMember()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var member = OrganizationMember.Create(orgId, userId, OrgRole.Admin);

        // Assert
        member.Id.Should().NotBeEmpty();
        member.OrganizationId.Should().Be(orgId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(OrgRole.Admin);
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyOrgId_ShouldThrow()
    {
        // Act & Assert
        var act = () => OrganizationMember.Create(Guid.Empty, "user-id", OrgRole.Moderator);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidUserId_ShouldThrow(string? userId)
    {
        // Act & Assert
        var act = () => OrganizationMember.Create(Guid.NewGuid(), userId!, OrgRole.Admin);
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Implement OrganizationMember entity**

```csharp
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string UserId { get; private set; } = null!;
    public OrgRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private OrganizationMember() { }

    public static OrganizationMember Create(Guid organizationId, string userId, OrgRole role)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "OrganizationMemberTests"`
Expected: All pass

- [ ] **Step 4: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Entities/OrganizationMember.cs tests/UserService/PetAdoption.UserService.UnitTests/Domain/OrganizationMemberTests.cs
git commit -m "add OrganizationMember entity with unit tests"
```

---

### Task 5: Create repository interfaces

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationRepository.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationMemberRepository.cs`

- [ ] **Step 1: Create IOrganizationRepository**

```csharp
using PetAdoption.UserService.Domain.Entities;

namespace PetAdoption.UserService.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id);
    Task<Organization?> GetBySlugAsync(string slug);
    Task<(IEnumerable<Organization> Items, long Total)> GetAllAsync(int skip, int take);
    Task AddAsync(Organization organization);
    Task UpdateAsync(Organization organization);
}
```

- [ ] **Step 2: Create IOrganizationMemberRepository**

```csharp
using PetAdoption.UserService.Domain.Entities;

namespace PetAdoption.UserService.Domain.Interfaces;

public interface IOrganizationMemberRepository
{
    Task AddAsync(OrganizationMember member);
    Task<OrganizationMember?> GetByOrgAndUserAsync(Guid organizationId, string userId);
    Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid organizationId);
    Task<IEnumerable<OrganizationMember>> GetByUserAsync(string userId);
    Task DeleteAsync(OrganizationMember member);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationRepository.cs src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IOrganizationMemberRepository.cs
git commit -m "add organization repository interfaces"
```

---

## Chunk 2: UserService Infrastructure Layer

### Task 6: Add EF Core entity configurations

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/UserServiceDbContext.cs`

- [ ] **Step 1: Add DbSets and entity configurations**

Add to `UserServiceDbContext`:

```csharp
public DbSet<Organization> Organizations => Set<Organization>();
public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
```

Add to `OnModelCreating`:

```csharp
modelBuilder.Entity<Organization>(entity =>
{
    entity.ToTable("Organizations");
    entity.HasKey(o => o.Id);
    entity.Property(o => o.Name).HasMaxLength(100).IsRequired();
    entity.Property(o => o.Slug).HasMaxLength(50).IsRequired();
    entity.HasIndex(o => o.Slug).IsUnique();
    entity.Property(o => o.Description).HasMaxLength(500);
    entity.Property(o => o.Status).HasConversion<int>();
});

modelBuilder.Entity<OrganizationMember>(entity =>
{
    entity.ToTable("OrganizationMembers");
    entity.HasKey(m => m.Id);
    entity.Property(m => m.UserId).HasMaxLength(450).IsRequired();
    entity.Property(m => m.Role).HasConversion<int>();
    entity.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();
});
```

Add the required usings at the top.

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.Infrastructure`

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/UserServiceDbContext.cs
git commit -m "add EF Core entity configs for Organization and OrganizationMember"
```

---

### Task 7: Implement repositories

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationRepository.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationMemberRepository.cs`

- [ ] **Step 1: Implement OrganizationRepository**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Infrastructure.Persistence;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly UserServiceDbContext _db;

    public OrganizationRepository(UserServiceDbContext db) => _db = db;

    public async Task<Organization?> GetByIdAsync(Guid id) =>
        await _db.Organizations.FindAsync(id);

    public async Task<Organization?> GetBySlugAsync(string slug) =>
        await _db.Organizations.FirstOrDefaultAsync(o => o.Slug == slug);

    public async Task<(IEnumerable<Organization> Items, long Total)> GetAllAsync(int skip, int take)
    {
        var total = await _db.Organizations.LongCountAsync();
        var items = await _db.Organizations.OrderBy(o => o.Name).Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Organization organization)
    {
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Organization organization)
    {
        _db.Organizations.Update(organization);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Implement OrganizationMemberRepository**

```csharp
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Infrastructure.Persistence;

public class OrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly UserServiceDbContext _db;

    public OrganizationMemberRepository(UserServiceDbContext db) => _db = db;

    public async Task AddAsync(OrganizationMember member)
    {
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync();
    }

    public async Task<OrganizationMember?> GetByOrgAndUserAsync(Guid organizationId, string userId) =>
        await _db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);

    public async Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid organizationId) =>
        await _db.OrganizationMembers.Where(m => m.OrganizationId == organizationId).ToListAsync();

    public async Task<IEnumerable<OrganizationMember>> GetByUserAsync(string userId) =>
        await _db.OrganizationMembers.Where(m => m.UserId == userId).ToListAsync();

    public async Task DeleteAsync(OrganizationMember member)
    {
        _db.OrganizationMembers.Remove(member);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Register in DI**

In `src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, add:

```csharp
services.AddScoped<IOrganizationRepository, OrganizationRepository>();
services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.Infrastructure`

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationRepository.cs src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OrganizationMemberRepository.cs src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
git commit -m "implement organization repositories and register in DI"
```

---

### Task 8: Add PlatformAdminOnly auth policy

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.API/Program.cs`

- [ ] **Step 1: Add PlatformAdminOnly policy and update existing policies**

Find the `AddAuthorization` block and update:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "PlatformAdmin"));
    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireRole("User", "Admin", "PlatformAdmin"));
    options.AddPolicy("PlatformAdminOnly", policy =>
        policy.RequireRole("PlatformAdmin"));
});
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.API`

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.API/Program.cs
git commit -m "add PlatformAdminOnly auth policy and update existing policies"
```

---

## Chunk 3: UserService Application + API Layer

### Task 9: Create Organization command handlers

**Files:**
- Create commands and handlers in `src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/`

- [ ] **Step 1: Create CreateOrganizationCommand**

```csharp
namespace PetAdoption.UserService.Application.Commands.Organizations;

public record CreateOrganizationCommand(string Name, string Slug, string? Description);
public record CreateOrganizationResponse(bool Success, Guid OrganizationId, string Message);
```

- [ ] **Step 2: Create CreateOrganizationCommandHandler**

```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class CreateOrganizationCommandHandler : ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;

    public CreateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<CreateOrganizationResponse> HandleAsync(CreateOrganizationCommand command)
    {
        var existing = await _orgRepo.GetBySlugAsync(command.Slug);
        if (existing is not null)
            return new CreateOrganizationResponse(false, Guid.Empty, "Organization with this slug already exists");

        var org = Organization.Create(command.Name, command.Slug, command.Description);
        await _orgRepo.AddAsync(org);
        return new CreateOrganizationResponse(true, org.Id, "Organization created successfully");
    }
}
```

- [ ] **Step 3: Create UpdateOrganizationCommand + handler**

```csharp
namespace PetAdoption.UserService.Application.Commands.Organizations;

public record UpdateOrganizationCommand(Guid Id, string Name, string? Description);
public record UpdateOrganizationResponse(bool Success, string Message);
```

```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class UpdateOrganizationCommandHandler : ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;

    public UpdateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<UpdateOrganizationResponse> HandleAsync(UpdateOrganizationCommand command)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null)
            return new UpdateOrganizationResponse(false, "Organization not found");

        org.Update(command.Name, command.Description);
        await _orgRepo.UpdateAsync(org);
        return new UpdateOrganizationResponse(true, "Organization updated");
    }
}
```

- [ ] **Step 4: Create Deactivate/Activate handlers**

```csharp
// DeactivateOrganizationCommandHandler.cs
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record DeactivateOrganizationCommand(Guid Id);
public record DeactivateOrganizationResponse(bool Success, string Message);

public class DeactivateOrganizationCommandHandler : ICommandHandler<DeactivateOrganizationCommand, DeactivateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public DeactivateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<DeactivateOrganizationResponse> HandleAsync(DeactivateOrganizationCommand command)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null) return new DeactivateOrganizationResponse(false, "Organization not found");
        org.Deactivate();
        await _orgRepo.UpdateAsync(org);
        return new DeactivateOrganizationResponse(true, "Organization deactivated");
    }
}
```

```csharp
// ActivateOrganizationCommandHandler.cs
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record ActivateOrganizationCommand(Guid Id);
public record ActivateOrganizationResponse(bool Success, string Message);

public class ActivateOrganizationCommandHandler : ICommandHandler<ActivateOrganizationCommand, ActivateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public ActivateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<ActivateOrganizationResponse> HandleAsync(ActivateOrganizationCommand command)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null) return new ActivateOrganizationResponse(false, "Organization not found");
        org.Activate();
        await _orgRepo.UpdateAsync(org);
        return new ActivateOrganizationResponse(true, "Organization activated");
    }
}
```

- [ ] **Step 5: Create AddOrganizationMember command + handler**

```csharp
// AddOrganizationMemberCommand.cs
namespace PetAdoption.UserService.Application.Commands.Organizations;

public record AddOrganizationMemberCommand(Guid OrganizationId, string UserId, string Role);
public record AddOrganizationMemberResponse(bool Success, string Message);
```

```csharp
// AddOrganizationMemberCommandHandler.cs
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class AddOrganizationMemberCommandHandler : ICommandHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IOrganizationMemberRepository _memberRepo;

    public AddOrganizationMemberCommandHandler(IOrganizationRepository orgRepo, IOrganizationMemberRepository memberRepo)
    {
        _orgRepo = orgRepo;
        _memberRepo = memberRepo;
    }

    public async Task<AddOrganizationMemberResponse> HandleAsync(AddOrganizationMemberCommand command)
    {
        var org = await _orgRepo.GetByIdAsync(command.OrganizationId);
        if (org is null) return new AddOrganizationMemberResponse(false, "Organization not found");

        var existing = await _memberRepo.GetByOrgAndUserAsync(command.OrganizationId, command.UserId);
        if (existing is not null) return new AddOrganizationMemberResponse(false, "User is already a member");

        if (!Enum.TryParse<OrgRole>(command.Role, true, out var role))
            return new AddOrganizationMemberResponse(false, "Invalid role. Use 'Admin' or 'Moderator'");

        var member = OrganizationMember.Create(command.OrganizationId, command.UserId, role);
        await _memberRepo.AddAsync(member);
        return new AddOrganizationMemberResponse(true, "Member added");
    }
}
```

- [ ] **Step 6: Create RemoveOrganizationMember handler**

```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record RemoveOrganizationMemberCommand(Guid OrganizationId, string UserId);
public record RemoveOrganizationMemberResponse(bool Success, string Message);

public class RemoveOrganizationMemberCommandHandler : ICommandHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    public RemoveOrganizationMemberCommandHandler(IOrganizationMemberRepository memberRepo) => _memberRepo = memberRepo;

    public async Task<RemoveOrganizationMemberResponse> HandleAsync(RemoveOrganizationMemberCommand command)
    {
        var member = await _memberRepo.GetByOrgAndUserAsync(command.OrganizationId, command.UserId);
        if (member is null) return new RemoveOrganizationMemberResponse(false, "Member not found");
        await _memberRepo.DeleteAsync(member);
        return new RemoveOrganizationMemberResponse(true, "Member removed");
    }
}
```

- [ ] **Step 7: Register all handlers in DI**

In `ServiceCollectionExtensions.cs`, add:

```csharp
services.AddScoped<ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse>, CreateOrganizationCommandHandler>();
services.AddScoped<ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse>, UpdateOrganizationCommandHandler>();
services.AddScoped<ICommandHandler<DeactivateOrganizationCommand, DeactivateOrganizationResponse>, DeactivateOrganizationCommandHandler>();
services.AddScoped<ICommandHandler<ActivateOrganizationCommand, ActivateOrganizationResponse>, ActivateOrganizationCommandHandler>();
services.AddScoped<ICommandHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse>, AddOrganizationMemberCommandHandler>();
services.AddScoped<ICommandHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResponse>, RemoveOrganizationMemberCommandHandler>();
```

- [ ] **Step 8: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.API`

- [ ] **Step 9: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Application/Commands/Organizations/ src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
git commit -m "add organization command handlers and register in DI"
```

---

### Task 10: Create query handlers

- [ ] **Step 1: Create GetOrganizations, GetOrganizationById, GetOrganizationMembers, GetMyOrganizations query handlers**

Each follows the pattern: query record + response record + handler class, using `IQueryHandler<TQuery, TResponse>`, injecting repositories. Place in `src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/`.

GetOrganizationsQueryHandler:
```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationsQuery(int Skip = 0, int Take = 20);
public record OrganizationListItem(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
public record GetOrganizationsResponse(List<OrganizationListItem> Organizations, long Total, int Skip, int Take);

public class GetOrganizationsQueryHandler : IQueryHandler<GetOrganizationsQuery, GetOrganizationsResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public GetOrganizationsQueryHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<GetOrganizationsResponse> HandleAsync(GetOrganizationsQuery query)
    {
        var (items, total) = await _orgRepo.GetAllAsync(query.Skip, query.Take);
        var list = items.Select(o => new OrganizationListItem(o.Id, o.Name, o.Slug, o.Status == Domain.Enums.OrganizationStatus.Active, o.CreatedAt)).ToList();
        return new GetOrganizationsResponse(list, total, query.Skip, query.Take);
    }
}
```

GetOrganizationByIdQueryHandler:
```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationByIdQuery(Guid Id);
public record OrganizationDetailResponse(Guid Id, string Name, string Slug, string? Description, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public class GetOrganizationByIdQueryHandler : IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse?>
{
    private readonly IOrganizationRepository _orgRepo;
    public GetOrganizationByIdQueryHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<OrganizationDetailResponse?> HandleAsync(GetOrganizationByIdQuery query)
    {
        var org = await _orgRepo.GetByIdAsync(query.Id);
        if (org is null) return null;
        return new OrganizationDetailResponse(org.Id, org.Name, org.Slug, org.Description, org.Status == Domain.Enums.OrganizationStatus.Active, org.CreatedAt, org.UpdatedAt);
    }
}
```

GetOrganizationMembersQueryHandler:
```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationMembersQuery(Guid OrganizationId);
public record OrganizationMemberItem(Guid Id, Guid OrganizationId, string UserId, string Role, DateTime JoinedAt);
public record GetOrganizationMembersResponse(List<OrganizationMemberItem> Members);

public class GetOrganizationMembersQueryHandler : IQueryHandler<GetOrganizationMembersQuery, GetOrganizationMembersResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    public GetOrganizationMembersQueryHandler(IOrganizationMemberRepository memberRepo) => _memberRepo = memberRepo;

    public async Task<GetOrganizationMembersResponse> HandleAsync(GetOrganizationMembersQuery query)
    {
        var members = await _memberRepo.GetByOrganizationAsync(query.OrganizationId);
        var items = members.Select(m => new OrganizationMemberItem(m.Id, m.OrganizationId, m.UserId, m.Role.ToString(), m.JoinedAt)).ToList();
        return new GetOrganizationMembersResponse(items);
    }
}
```

GetMyOrganizationsQueryHandler:
```csharp
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetMyOrganizationsQuery(string UserId);
public record MyOrganizationItem(Guid OrganizationId, string OrganizationName, string Slug, string Role, DateTime JoinedAt);
public record GetMyOrganizationsResponse(List<MyOrganizationItem> Organizations);

public class GetMyOrganizationsQueryHandler : IQueryHandler<GetMyOrganizationsQuery, GetMyOrganizationsResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    private readonly IOrganizationRepository _orgRepo;

    public GetMyOrganizationsQueryHandler(IOrganizationMemberRepository memberRepo, IOrganizationRepository orgRepo)
    {
        _memberRepo = memberRepo;
        _orgRepo = orgRepo;
    }

    public async Task<GetMyOrganizationsResponse> HandleAsync(GetMyOrganizationsQuery query)
    {
        var memberships = await _memberRepo.GetByUserAsync(query.UserId);
        var items = new List<MyOrganizationItem>();
        foreach (var m in memberships)
        {
            var org = await _orgRepo.GetByIdAsync(m.OrganizationId);
            if (org is not null)
                items.Add(new MyOrganizationItem(org.Id, org.Name, org.Slug, m.Role.ToString(), m.JoinedAt));
        }
        return new GetMyOrganizationsResponse(items);
    }
}
```

- [ ] **Step 2: Register query handlers in DI**

- [ ] **Step 3: Verify build and commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Application/Queries/Organizations/ src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
git commit -m "add organization query handlers"
```

---

### Task 11: Create OrganizationsController

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.API/Controllers/OrganizationsController.cs`

- [ ] **Step 1: Create controller with all endpoints**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands.Organizations;
using PetAdoption.UserService.Application.Queries.Organizations;

namespace PetAdoption.UserService.API.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        [FromServices] ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new CreateOrganizationCommand(request.Name, request.Slug, request.Description));
        if (!result.Success) return Conflict(result);
        return Created($"/api/organizations/{result.OrganizationId}", result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromServices] IQueryHandler<GetOrganizationsQuery, GetOrganizationsResponse> handler = null!)
    {
        var result = await handler.HandleAsync(new GetOrganizationsQuery(skip, take));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse?> handler)
    {
        var result = await handler.HandleAsync(new GetOrganizationByIdQuery(id));
        if (result is null) return NotFound();
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        [FromServices] ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new UpdateOrganizationCommand(id, request.Name, request.Description));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] ICommandHandler<DeactivateOrganizationCommand, DeactivateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new DeactivateOrganizationCommand(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromServices] ICommandHandler<ActivateOrganizationCommand, ActivateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new ActivateOrganizationCommand(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(
        Guid id,
        [FromBody] AddMemberRequest request,
        [FromServices] ICommandHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse> handler)
    {
        var result = await handler.HandleAsync(new AddOrganizationMemberCommand(id, request.UserId, request.Role));
        if (!result.Success) return Conflict(result);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(
        Guid id,
        [FromServices] IQueryHandler<GetOrganizationMembersQuery, GetOrganizationMembersResponse> handler)
    {
        var result = await handler.HandleAsync(new GetOrganizationMembersQuery(id));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        string userId,
        [FromServices] ICommandHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResponse> handler)
    {
        var result = await handler.HandleAsync(new RemoveOrganizationMemberCommand(id, userId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrganizations(
        [FromServices] IQueryHandler<GetMyOrganizationsQuery, GetMyOrganizationsResponse> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found");
        var result = await handler.HandleAsync(new GetMyOrganizationsQuery(userId));
        return Ok(result);
    }
}

public record CreateOrganizationRequest(string Name, string Slug, string? Description);
public record UpdateOrganizationRequest(string Name, string? Description);
public record AddMemberRequest(string UserId, string Role);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.API`

- [ ] **Step 3: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.API/Controllers/OrganizationsController.cs
git commit -m "add OrganizationsController with full CRUD and member management"
```

---

## Chunk 4: PetService Domain Changes

### Task 12: Add OrganizationId to Pet aggregate

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs`

- [ ] **Step 1: Add OrganizationId property and AssignToOrganization method to Pet**

Add after the `Description` property:

```csharp
public Guid? OrganizationId { get; private set; }
```

Add method after `UpdateDescription`:

```csharp
public void AssignToOrganization(Guid organizationId)
{
    if (organizationId == Guid.Empty)
        throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
    OrganizationId = organizationId;
}
```

- [ ] **Step 2: Add OrganizationId to PetServiceDbContext**

Inside the `modelBuilder.Entity<Pet>` block, add:

```csharp
entity.Property(p => p.OrganizationId);
entity.HasIndex(p => p.OrganizationId);
```

- [ ] **Step 3: Run existing tests to verify no regressions**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`
Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs
git commit -m "add OrganizationId to Pet aggregate and EF Core mapping"
```

---

### Task 13: Update PetService AdminOnly policy

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Program.cs`

- [ ] **Step 1: Update AdminOnly policy to include PlatformAdmin**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "PlatformAdmin"));
});
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Program.cs
git commit -m "update PetService AdminOnly policy to include PlatformAdmin"
```

---

## Chunk 5: Integration Tests + Final Verification

### Task 14: Integration tests for organization management

**Files:**
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Tests/OrganizationManagementTests.cs`
- Create: `tests/UserService/PetAdoption.UserService.IntegrationTests/Builders/CreateOrganizationRequestBuilder.cs`

- [ ] **Step 1: Create builder**

```csharp
namespace PetAdoption.UserService.IntegrationTests.Builders;

public class CreateOrganizationRequestBuilder
{
    private string _name = "Test Organization";
    private string _slug = $"test-org-{Guid.NewGuid():N}"[..30];
    private string? _description = null;

    public CreateOrganizationRequestBuilder WithName(string name) { _name = name; return this; }
    public CreateOrganizationRequestBuilder WithSlug(string slug) { _slug = slug; return this; }
    public CreateOrganizationRequestBuilder WithDescription(string? desc) { _description = desc; return this; }
    public object Build() => new { Name = _name, Slug = _slug, Description = _description };
    public static CreateOrganizationRequestBuilder Default() => new();
}
```

- [ ] **Step 2: Write integration tests**

Tests should cover: create org as PlatformAdmin (201), create as regular user (403), create with duplicate slug (409), list orgs, get by ID, deactivate/activate, add member, list members, get my orgs.

Follow existing test patterns: `[Collection("SqlServer")]`, `IAsyncLifetime`, `WebApplicationFactory<Program>`, promote to PlatformAdmin via direct SQL (`UPDATE Users SET Role = 2 WHERE Id = ...`), private response DTOs at bottom of test class.

- [ ] **Step 3: Run integration tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests --filter "OrganizationManagementTests"`

- [ ] **Step 4: Commit**

```bash
git add tests/UserService/PetAdoption.UserService.IntegrationTests/
git commit -m "add organization management integration tests"
```

---

### Task 15: Full build and test run

- [ ] **Step 1: Build entire solution**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 2: Run all tests**

Run: `dotnet test PetAdoption.sln`

- [ ] **Step 3: Commit if any fixes needed**

---

## Design Decisions

1. **Organization lives in UserService** -- it's about user membership, not pets.
2. **Pet.OrganizationId is nullable** -- backward-compatible with existing pets that have no org.
3. **PlatformAdmin is a UserRole** (global). OrgAdmin/OrgModerator are OrgRoles (per-org membership).
4. **JWT tokens are NOT changed in this plan** -- org membership is checked via database queries. A future optimization could add org claims to JWT.
5. **PlatformAdminOnly is a separate policy** from AdminOnly. AdminOnly now also accepts PlatformAdmin.
6. **Slug is immutable after creation** -- prevents breaking cross-service references.

## New API Endpoints Summary

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/organizations` | PlatformAdmin | Create org |
| GET | `/api/organizations` | PlatformAdmin | List orgs (paginated) |
| GET | `/api/organizations/{id}` | PlatformAdmin | Get org by ID |
| PUT | `/api/organizations/{id}` | PlatformAdmin | Update org |
| POST | `/api/organizations/{id}/deactivate` | PlatformAdmin | Deactivate |
| POST | `/api/organizations/{id}/activate` | PlatformAdmin | Activate |
| POST | `/api/organizations/{id}/members` | PlatformAdmin | Add member |
| GET | `/api/organizations/{id}/members` | PlatformAdmin | List members |
| DELETE | `/api/organizations/{id}/members/{userId}` | PlatformAdmin | Remove member |
| GET | `/api/organizations/mine` | Authenticated | My orgs |
