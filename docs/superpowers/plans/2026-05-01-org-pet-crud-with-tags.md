# Organization Pet CRUD with Tags Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add tag management to Pet entities and introduce organization-scoped pet CRUD endpoints so OrgAdmin/OrgModerator users can manage pets belonging to their organization.

**Architecture:** Extends Pet aggregate with a `List<PetTag>` collection (stored as JSON column in SQL Server). A new `OrgPetsController` provides organization-scoped CRUD at `/api/organizations/{orgId}/pets`, authorized via JWT claims (`organizationId`, `orgRole`). Existing public `PetsController` GET endpoints remain anonymous and are extended with tag filtering. The Blazor frontend gains an organization pet management page with MudChipSet-based tag editing.

**Tech Stack:** .NET 9.0 (PetService), EF Core + SQL Server, xUnit + FluentAssertions + Testcontainers, MudBlazor 8.x

**Dependencies:** Plan 4 (Organization Management + Platform Admin) must be completed first. This plan assumes Pet already has `OrganizationId` (Guid) property, JWT tokens contain `organizationId` and `orgRole` claims, and `OrgRole` enum values `Admin` and `Moderator` exist.

---

## File Structure

### New files:
- `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetTag.cs` -- PetTag value object
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommand.cs` -- Org-scoped create pet command
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommandHandler.cs` -- Org-scoped create pet handler + response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdateOrgPetCommand.cs` -- Org-scoped update pet command, handler, response
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/DeleteOrgPetCommand.cs` -- Org-scoped delete pet command, handler, response
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgPetsQuery.cs` -- Org-scoped list pets query, handler, response
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/OrgPetsController.cs` -- Organization-scoped pet CRUD controller
- `src/Services/PetService/PetAdoption.PetService.API/Authorization/OrgAuthorizationFilter.cs` -- Action filter for org role checks
- `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagTests.cs` -- PetTag value object unit tests
- `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagsOnPetTests.cs` -- Pet tag management unit tests
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/OrgPetsControllerTests.cs` -- Integration tests for org-scoped endpoints
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreateOrgPetRequestBuilder.cs` -- Builder for org pet create requests
- `src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/ManageOrgPets.razor` -- Org pet management page

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs` -- Add `Tags` property and tag management methods
- `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` -- Add `InvalidPetTag` error code
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs` -- Add JSON column mapping for Tags
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs` -- Map new error codes
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs` -- Add `GetFilteredByOrg()` and tag-filtered overload
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs` -- Add `Tags` filter parameter
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs` -- Implement org-filtered and tag-filtered queries
- `src/Services/PetService/PetAdoption.PetService.Application/DTOs/PetListItemDto.cs` -- Add `Tags` field
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetByIdQuery.cs` -- Add `Tags` to `PetDetailsDto`
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommand.cs` -- Add `Tags` parameter
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommandHandler.cs` -- Pass tags to `Pet.Create()`
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs` -- Add `Tags` parameter, call `SetTags()`
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs` -- Add `tags` query param to GET, add tags to request DTOs
- `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` -- Add tags to pet models
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` -- Add org-scoped API methods
- `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/PetFormDialog.razor` -- Add tag input

---

## Chunk 1: PetTag Value Object

### Task 1.1: Create PetTag value object

**Files:**
- New: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetTag.cs`

- [ ] **Step 1: Write PetTag value object unit tests (TDD)**

Create `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagTests.cs`:

```csharp
using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTagTests
{
    // ──────────────────────────────────────────────────────────────
    // Valid Creation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("friendly", "friendly")]
    [InlineData("Friendly", "friendly")]
    [InlineData("VACCINATED", "vaccinated")]
    [InlineData("house-trained", "house-trained")]
    [InlineData("good-with-kids", "good-with-kids")]
    [InlineData("  special-needs  ", "special-needs")]
    public void Constructor_WithValidTag_ShouldNormalizeToLowerTrimmed(string input, string expected)
    {
        // Act
        var tag = new PetTag(input);

        // Assert
        tag.Value.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithMaxLengthTag_ShouldSucceed()
    {
        // Arrange
        var maxTag = new string('a', PetTag.MaxLength);

        // Act
        var tag = new PetTag(maxTag);

        // Assert
        tag.Value.Should().Be(maxTag);
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid Creation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithNullOrWhitespace_ShouldThrowDomainException(string? invalidTag)
    {
        // Act
        var act = () => new PetTag(invalidTag!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetTag);
    }

    [Fact]
    public void Constructor_WithTagExceedingMaxLength_ShouldThrowDomainException()
    {
        // Arrange
        var tooLong = new string('a', PetTag.MaxLength + 1);

        // Act
        var act = () => new PetTag(tooLong);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetTag);
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("Friendly");

        // Act & Assert
        tag1.Equals(tag2).Should().BeTrue();
        (tag1 == tag2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("vaccinated");

        // Act & Assert
        tag1.Equals(tag2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ForEqualValues_ShouldBeSame()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("FRIENDLY");

        // Act & Assert
        tag1.GetHashCode().Should().Be(tag2.GetHashCode());
    }

    // ──────────────────────────────────────────────────────────────
    // String Conversion
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var tag = new PetTag("friendly");

        // Act & Assert
        tag.ToString().Should().Be("friendly");
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        // Arrange
        var tag = new PetTag("vaccinated");

        // Act
        string result = tag;

        // Assert
        result.Should().Be("vaccinated");
    }
}
```

- [ ] **Step 2: Implement PetTag value object**

Create `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetTag.cs`:

```csharp
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

/// <summary>
/// Value object representing a pet tag. Normalized to lowercase, trimmed, 1-50 chars.
/// </summary>
public sealed class PetTag : IEquatable<PetTag>
{
    public const int MaxLength = 50;
    public const int MinLength = 1;

    public string Value { get; }

    public PetTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                "Pet tag cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value ?? "null" },
                    { "Reason", "EmptyOrWhitespace" }
                });
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                $"Pet tag must be at least {MinLength} character(s).",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MinLength", MinLength },
                    { "ActualLength", normalized.Length },
                    { "Reason", "TooShort" }
                });
        }

        if (normalized.Length > MaxLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                $"Pet tag cannot exceed {MaxLength} characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MaxLength", MaxLength },
                    { "ActualLength", normalized.Length },
                    { "Reason", "TooLong" }
                });
        }

        Value = normalized;
    }

    public bool Equals(PetTag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as PetTag);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(PetTag tag) => tag.Value;
    public static explicit operator PetTag(string value) => new(value);

    public static bool operator ==(PetTag? left, PetTag? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PetTag? left, PetTag? right) => !(left == right);
}
```

- [ ] **Step 3: Add `InvalidPetTag` error code**

Add to `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs` after the `InvalidPetDescription` block:

```csharp
/// <summary>
/// Pet tag is invalid (empty, too long, etc.).
/// </summary>
public const string InvalidPetTag = "invalid_pet_tag";
```

- [ ] **Step 4: Run unit tests**

```bash
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
```

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetTag.cs src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagTests.cs
git commit -m "add PetTag value object with validation and unit tests"
```

---

## Chunk 2: Tags on Pet Aggregate

### Task 2.1: Add Tags property and management methods to Pet

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`

- [ ] **Step 1: Write unit tests for tag management on Pet (TDD)**

Create `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagsOnPetTests.cs`:

```csharp
using FluentAssertions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTagsOnPetTests
{
    private static readonly Guid TestPetTypeId = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────
    // Create with Tags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithTags_ShouldSetTags()
    {
        // Arrange
        var tags = new[] { "friendly", "vaccinated" };

        // Act
        var pet = Pet.Create("Bella", TestPetTypeId, tags: tags);

        // Assert
        pet.Tags.Should().HaveCount(2);
        pet.Tags.Select(t => t.Value).Should().Contain("friendly");
        pet.Tags.Select(t => t.Value).Should().Contain("vaccinated");
    }

    [Fact]
    public void Create_WithNullTags_ShouldHaveEmptyTags()
    {
        // Act
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDuplicateTags_ShouldDedup()
    {
        // Arrange
        var tags = new[] { "friendly", "Friendly", "FRIENDLY" };

        // Act
        var pet = Pet.Create("Bella", TestPetTypeId, tags: tags);

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("friendly");
    }

    // ──────────────────────────────────────────────────────────────
    // AddTag
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddTag_WithNewTag_ShouldAddTag()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Act
        pet.AddTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("friendly");
    }

    [Fact]
    public void AddTag_WithDuplicateTag_ShouldNotAddDuplicate()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.AddTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void AddTag_CaseInsensitiveDuplicate_ShouldNotAdd()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.AddTag("FRIENDLY");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // RemoveTag
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveTag_WithExistingTag_ShouldRemove()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly", "vaccinated" });

        // Act
        pet.RemoveTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("vaccinated");
    }

    [Fact]
    public void RemoveTag_CaseInsensitive_ShouldRemove()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.RemoveTag("FRIENDLY");

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_NonExistentTag_ShouldDoNothing()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.RemoveTag("vaccinated");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // SetTags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetTags_ShouldReplaceAllTags()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly", "vaccinated" });

        // Act
        pet.SetTags(new[] { "neutered", "house-trained" });

        // Assert
        pet.Tags.Should().HaveCount(2);
        pet.Tags.Select(t => t.Value).Should().BeEquivalentTo(new[] { "neutered", "house-trained" });
    }

    [Fact]
    public void SetTags_WithEmptyList_ShouldClearTags()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.SetTags(Enumerable.Empty<string>());

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SetTags_WithDuplicates_ShouldDedup()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Act
        pet.SetTags(new[] { "friendly", "Friendly" });

        // Assert
        pet.Tags.Should().HaveCount(1);
    }
}
```

- [ ] **Step 2: Add Tags property and methods to Pet aggregate**

Modify `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`:

Add `using PetAdoption.PetService.Domain.ValueObjects;` at the top.

Add after the `Description` property:

```csharp
private readonly List<PetTag> _tags = new();
public IReadOnlyList<PetTag> Tags => _tags.AsReadOnly();
```

Update the private constructor to accept tags:

```csharp
private Pet(Guid id, PetName name, Guid petTypeId, PetBreed? breed = null, PetAge? age = null,
    PetDescription? description = null, IEnumerable<PetTag>? tags = null)
{
    if (id == Guid.Empty)
        throw new ArgumentException("Pet ID cannot be empty.", nameof(id));
    if (petTypeId == Guid.Empty)
        throw new ArgumentException("Pet type ID cannot be empty.", nameof(petTypeId));

    Id = id;
    Name = name ?? throw new ArgumentNullException(nameof(name));
    PetTypeId = petTypeId;
    Breed = breed;
    Age = age;
    Description = description;
    Status = PetStatus.Available;

    if (tags is not null)
    {
        foreach (var tag in tags)
        {
            if (!_tags.Contains(tag))
                _tags.Add(tag);
        }
    }
}
```

Update the factory method (backward-compatible via default parameter):

```csharp
public static Pet Create(string name, Guid petTypeId, string? breed = null, int? ageMonths = null,
    string? description = null, IEnumerable<string>? tags = null)
{
    return new Pet(
        Guid.NewGuid(),
        new PetName(name),
        petTypeId,
        breed is not null ? new PetBreed(breed) : null,
        ageMonths.HasValue ? new PetAge(ageMonths.Value) : null,
        description is not null ? new PetDescription(description) : null,
        tags?.Select(t => new PetTag(t)));
}
```

Add tag management methods after `UpdateDescription()`:

```csharp
public void AddTag(string tag)
{
    var petTag = new PetTag(tag);
    if (!_tags.Contains(petTag))
        _tags.Add(petTag);
}

public void RemoveTag(string tag)
{
    var petTag = new PetTag(tag);
    _tags.Remove(petTag);
}

public void SetTags(IEnumerable<string> tags)
{
    _tags.Clear();
    foreach (var tag in tags)
    {
        var petTag = new PetTag(tag);
        if (!_tags.Contains(petTag))
            _tags.Add(petTag);
    }
}
```

- [ ] **Step 3: Run all unit tests**

```bash
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
```

Verify existing tests still pass (the factory method signature is backward-compatible via default parameter).

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTagsOnPetTests.cs
git commit -m "add tag management to Pet aggregate"
```

---

## Chunk 3: EF Core Mapping for Tags

### Task 3.1: Add JSON column mapping for Tags

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs`

- [ ] **Step 1: Add JSON conversion for Tags property**

Add `using System.Text.Json;` at the top of `PetServiceDbContext.cs`.

Add the following inside the `modelBuilder.Entity<Pet>(entity => { ... })` block, after the `entity.Ignore(p => p.DomainEvents);` line:

```csharp
entity.Property(p => p.Tags)
    .HasField("_tags")
    .HasConversion(
        v => JsonSerializer.Serialize(v.Select(t => t.Value).ToList(), (JsonSerializerOptions?)null),
        v => string.IsNullOrEmpty(v)
            ? new List<PetTag>()
            : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!
                .Select(s => new PetTag(s)).ToList())
    .HasColumnName("Tags")
    .HasColumnType("nvarchar(max)")
    .IsRequired(false);
```

**Note:** Since the project uses `EnsureCreatedAsync()` (no migrations), the Tags column will be created automatically on next startup. Existing rows will have NULL for Tags. The domain `_tags` field initializes to an empty list, so the conversion from NULL is handled by the `string.IsNullOrEmpty` check.

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetServiceDbContext.cs
git commit -m "add EF Core JSON column mapping for Pet tags"
```

---

## Chunk 4: Update DTOs and Existing Commands/Queries for Tags

### Task 4.1: Add Tags to DTOs

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/DTOs/PetListItemDto.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetByIdQuery.cs`

- [ ] **Step 1: Add Tags to PetListItemDto**

Update `PetListItemDto` in `src/Services/PetService/PetAdoption.PetService.Application/DTOs/PetListItemDto.cs`:

```csharp
namespace PetAdoption.PetService.Application.DTOs;

public record PetListItemDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    string? Breed,
    int? AgeMonths,
    string? Description,
    List<string> Tags
);
```

- [ ] **Step 2: Add Tags to PetDetailsDto**

Update `PetDetailsDto` in the handler file for GetPetByIdQuery:

```csharp
public record PetDetailsDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    string? Breed,
    int? AgeMonths,
    string? Description,
    List<string> Tags
);
```

- [ ] **Step 3: Update GetPetByIdQueryHandler to include Tags**

Update the return statement in `Handle()`:

```csharp
return new PetDetailsDto(
    pet.Id,
    pet.Name,
    petTypeName,
    pet.Status.ToString(),
    pet.Breed?.Value,
    pet.Age?.Months,
    pet.Description?.Value,
    pet.Tags.Select(t => t.Value).ToList()
);
```

- [ ] **Step 4: Update GetPetsQueryHandler to include Tags**

In the `GetPetsQuery` handler, update the DTO mapping in `Handle()`:

```csharp
var items = pets.Select(p => new PetListItemDto(
    p.Id,
    p.Name,
    petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
    p.Status.ToString(),
    p.Breed?.Value,
    p.Age?.Months,
    p.Description?.Value,
    p.Tags.Select(t => t.Value).ToList()
)).ToList();
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.Application
```

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/DTOs/PetListItemDto.cs src/Services/PetService/PetAdoption.PetService.Application/Queries/
git commit -m "add tags to pet DTOs and query handlers"
```

---

### Task 4.2: Add Tags to existing Create/Update commands

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommand.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommandHandler.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs`

- [ ] **Step 1: Add Tags to CreatePetCommand**

Update `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetCommand(
    string Name,
    Guid PetTypeId,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<CreatePetResponse>;
```

- [ ] **Step 2: Update CreatePetCommandHandler to pass tags**

In `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommandHandler.cs`, update `Handle()`:

```csharp
public async Task<CreatePetResponse> Handle(CreatePetCommand request, CancellationToken cancellationToken = default)
{
    var pet = Pet.Create(request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags);
    await _petRepository.Add(pet);
    return new CreatePetResponse(pet.Id);
}
```

- [ ] **Step 3: Add Tags to UpdatePetCommand and handler**

In the UpdatePetCommand file, add Tags parameter:

```csharp
public record UpdatePetCommand(
    Guid PetId,
    string Name,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<UpdatePetResponse>;
```

In the handler's `Handle()` method, add after `pet.UpdateDescription(request.Description);`:

```csharp
if (request.Tags is not null)
    pet.SetTags(request.Tags);
```

- [ ] **Step 4: Update PetsController request records and endpoint calls**

In `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`, update request records at the bottom:

```csharp
public record CreatePetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdatePetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
```

Update the `Create` action to pass tags:

```csharp
var result = await _mediator.Send(new CreatePetCommand(request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags));
```

Update the `Update` action to pass tags:

```csharp
var result = await _mediator.Send(new UpdatePetCommand(id, request.Name, request.Breed, request.AgeMonths, request.Description, request.Tags));
```

- [ ] **Step 5: Build and run existing tests**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.API
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
```

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/ src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs
git commit -m "add tags support to create and update pet commands"
```

---

## Chunk 5: Tag-Based Filtering on GET /api/pets

### Task 5.1: Add tag filtering to GetPetsQuery and IPetQueryStore

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`

- [ ] **Step 1: Add tags parameter and GetFilteredByOrg to IPetQueryStore**

Update `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs`:

```csharp
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

public interface IPetQueryStore
{
    Task<IEnumerable<Pet>> GetAll();
    Task<Pet?> GetById(Guid id);
    Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
        PetStatus? status,
        Guid? petTypeId,
        int skip,
        int take,
        IEnumerable<string>? tags = null);
    Task<(IEnumerable<Pet> Pets, long Total)> GetFilteredByOrg(
        Guid organizationId,
        PetStatus? status,
        int skip,
        int take,
        IEnumerable<string>? tags = null);
}
```

- [ ] **Step 2: Add tags to GetPetsQuery**

Update `GetPetsQuery` record:

```csharp
public record GetPetsQuery(
    PetStatus? Status,
    Guid? PetTypeId,
    int Skip = 0,
    int Take = 20,
    IEnumerable<string>? Tags = null) : IRequest<GetPetsResponse>;
```

Update the `Handle()` method to pass tags:

```csharp
var (pets, total) = await _queryStore.GetFiltered(
    request.Status,
    request.PetTypeId,
    request.Skip,
    request.Take,
    request.Tags);
```

- [ ] **Step 3: Implement tag filtering in PetQueryStore**

Update `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`.

For tag filtering, since Tags are stored as a JSON column and EF Core may not translate LINQ queries against JSON, apply tag filtering in memory after the database query:

```csharp
public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status, Guid? petTypeId, int skip, int take, IEnumerable<string>? tags = null)
{
    var query = _db.Pets.AsNoTracking().AsQueryable();

    if (status.HasValue)
        query = query.Where(p => p.Status == status.Value);

    if (petTypeId.HasValue)
        query = query.Where(p => p.PetTypeId == petTypeId.Value);

    // Tag filtering in memory (JSON columns don't support LINQ Contains in EF Core SQL Server)
    if (tags is not null)
    {
        var tagList = tags.Select(t => t.Trim().ToLowerInvariant()).ToList();
        if (tagList.Count > 0)
        {
            var allPets = await query.ToListAsync();
            var filtered = allPets.Where(p => tagList.All(tag => p.Tags.Any(t => t.Value == tag))).ToList();
            return (filtered.Skip(skip).Take(take), filtered.Count);
        }
    }

    var total = await query.LongCountAsync();
    var pets = await query.Skip(skip).Take(take).ToListAsync();
    return (pets, total);
}

public async Task<(IEnumerable<Pet> Pets, long Total)> GetFilteredByOrg(
    Guid organizationId, PetStatus? status, int skip, int take, IEnumerable<string>? tags = null)
{
    var query = _db.Pets.AsNoTracking()
        .Where(p => p.OrganizationId == organizationId);

    if (status.HasValue)
        query = query.Where(p => p.Status == status.Value);

    if (tags is not null)
    {
        var tagList = tags.Select(t => t.Trim().ToLowerInvariant()).ToList();
        if (tagList.Count > 0)
        {
            var allPets = await query.ToListAsync();
            var filtered = allPets.Where(p => tagList.All(tag => p.Tags.Any(t => t.Value == tag))).ToList();
            return (filtered.Skip(skip).Take(take), filtered.Count);
        }
    }

    var total = await query.LongCountAsync();
    var pets = await query.Skip(skip).Take(take).ToListAsync();
    return (pets, total);
}
```

- [ ] **Step 4: Add tags query parameter to PetsController GET endpoint**

In `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`, update the `GetAll` action:

```csharp
[AllowAnonymous]
[HttpGet]
public async Task<ActionResult<GetPetsResponse>> GetAll(
    [FromQuery] string? status = null,
    [FromQuery] Guid? petTypeId = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20,
    [FromQuery] string? tags = null)
{
    PetStatus? petStatus = null;
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<PetStatus>(status, true, out var parsed))
    {
        petStatus = parsed;
    }

    IEnumerable<string>? tagList = null;
    if (!string.IsNullOrWhiteSpace(tags))
    {
        tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    var result = await _mediator.Send(new GetPetsQuery(petStatus, petTypeId, skip, take, tagList));
    return Ok(result);
}
```

- [ ] **Step 5: Build and test**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.API
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
```

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/ src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs
git commit -m "add tag-based filtering to GET /api/pets endpoint"
```

---

## Chunk 6: Organization-Scoped Pet CRUD

### Task 6.1: Organization authorization filter

**Files:**
- New: `src/Services/PetService/PetAdoption.PetService.API/Authorization/OrgAuthorizationFilter.cs`

- [ ] **Step 1: Create OrgAuthorizationFilter**

This action filter reads `organizationId` and `orgRole` claims from JWT, validates the user belongs to the org from the route `{orgId}`, and that they have OrgAdmin or OrgModerator role.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PetAdoption.PetService.API.Authorization;

/// <summary>
/// Action filter that validates the authenticated user is a member of the organization
/// specified in the route parameter {orgId} with OrgAdmin or OrgModerator role.
/// Expects JWT claims: "organizationId" and "orgRole".
/// </summary>
public class OrgAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get orgId from route
        if (!context.RouteData.Values.TryGetValue("orgId", out var orgIdValue) ||
            !Guid.TryParse(orgIdValue?.ToString(), out var routeOrgId))
        {
            context.Result = new BadRequestObjectResult(new { error = "Invalid organization ID in route" });
            return;
        }

        // Check user's org membership from JWT claims
        var userOrgIdClaim = user.FindFirst("organizationId")?.Value;
        var userOrgRoleClaim = user.FindFirst("orgRole")?.Value;

        if (string.IsNullOrEmpty(userOrgIdClaim) ||
            !Guid.TryParse(userOrgIdClaim, out var userOrgId) ||
            userOrgId != routeOrgId)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check org role (Admin or Moderator)
        if (string.IsNullOrEmpty(userOrgRoleClaim) ||
            (userOrgRoleClaim != "Admin" && userOrgRoleClaim != "Moderator"))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Authorization/OrgAuthorizationFilter.cs
git commit -m "add organization authorization filter for org-scoped endpoints"
```

---

### Task 6.2: Organization-scoped pet commands and queries

**Files:**
- New: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommand.cs`
- New: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommandHandler.cs`
- New: `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdateOrgPetCommand.cs`
- New: `src/Services/PetService/PetAdoption.PetService.Application/Commands/DeleteOrgPetCommand.cs`
- New: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgPetsQuery.cs`

- [ ] **Step 1: Create CreateOrgPetCommand**

Create `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreateOrgPetCommand(
    Guid OrganizationId,
    string Name,
    Guid PetTypeId,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<CreateOrgPetResponse>;
```

- [ ] **Step 2: Create CreateOrgPetCommandHandler**

Create `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommandHandler.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreateOrgPetResponse(Guid Id);

public class CreateOrgPetCommandHandler : IRequestHandler<CreateOrgPetCommand, CreateOrgPetResponse>
{
    private readonly IPetRepository _petRepository;

    public CreateOrgPetCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<CreateOrgPetResponse> Handle(CreateOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = Pet.Create(
            request.Name,
            request.PetTypeId,
            request.Breed,
            request.AgeMonths,
            request.Description,
            request.Tags);

        pet.AssignToOrganization(request.OrganizationId);

        await _petRepository.Add(pet);
        return new CreateOrgPetResponse(pet.Id);
    }
}
```

**Note:** This assumes `Pet.AssignToOrganization(Guid orgId)` exists from Plan 4. If Plan 4 added `OrganizationId` as a constructor/Create parameter instead, adjust accordingly.

- [ ] **Step 3: Create UpdateOrgPetCommand (command + handler + response)**

Create `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdateOrgPetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdateOrgPetCommand(
    Guid OrganizationId,
    Guid PetId,
    string Name,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<UpdateOrgPetResponse>;

public record UpdateOrgPetResponse(
    Guid Id, string Name, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);

public class UpdateOrgPetCommandHandler : IRequestHandler<UpdateOrgPetCommand, UpdateOrgPetResponse>
{
    private readonly IPetRepository _repository;

    public UpdateOrgPetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateOrgPetResponse> Handle(UpdateOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });
        }

        if (pet.OrganizationId != request.OrganizationId)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found in organization {request.OrganizationId}.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId },
                    { "OrganizationId", request.OrganizationId }
                });
        }

        pet.UpdateName(request.Name);
        pet.UpdateBreed(request.Breed);
        pet.UpdateAge(request.AgeMonths);
        pet.UpdateDescription(request.Description);

        if (request.Tags is not null)
            pet.SetTags(request.Tags);

        await _repository.Update(pet);

        return new UpdateOrgPetResponse(
            pet.Id, pet.Name, pet.Status.ToString(), pet.Breed?.Value, pet.Age?.Months, pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList());
    }
}
```

- [ ] **Step 4: Create DeleteOrgPetCommand (command + handler + response)**

Create `src/Services/PetService/PetAdoption.PetService.Application/Commands/DeleteOrgPetCommand.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeleteOrgPetCommand(Guid OrganizationId, Guid PetId) : IRequest<DeleteOrgPetResponse>;

public record DeleteOrgPetResponse(bool Success, string Message);

public class DeleteOrgPetCommandHandler : IRequestHandler<DeleteOrgPetCommand, DeleteOrgPetResponse>
{
    private readonly IPetRepository _repository;

    public DeleteOrgPetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteOrgPetResponse> Handle(DeleteOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });
        }

        if (pet.OrganizationId != request.OrganizationId)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found in organization {request.OrganizationId}.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId },
                    { "OrganizationId", request.OrganizationId }
                });
        }

        pet.EnsureCanBeDeleted();
        await _repository.Delete(pet.Id);

        return new DeleteOrgPetResponse(true, $"Pet '{pet.Name}' has been deleted.");
    }
}
```

- [ ] **Step 5: Create GetOrgPetsQuery (query + handler + response)**

Create `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgPetsQuery.cs`:

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgPetsQuery(
    Guid OrganizationId,
    PetStatus? Status = null,
    int Skip = 0,
    int Take = 20,
    IEnumerable<string>? Tags = null) : IRequest<GetOrgPetsResponse>;

public record GetOrgPetsResponse(
    List<PetListItemDto> Pets, long Total, int Skip, int Take);

public class GetOrgPetsQueryHandler : IRequestHandler<GetOrgPetsQuery, GetOrgPetsResponse>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetOrgPetsQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetOrgPetsResponse> Handle(GetOrgPetsQuery request, CancellationToken cancellationToken = default)
    {
        var (pets, total) = await _queryStore.GetFilteredByOrg(
            request.OrganizationId, request.Status, request.Skip, request.Take, request.Tags);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = pets.Select(p => new PetListItemDto(
            p.Id, p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString(), p.Breed?.Value, p.Age?.Months, p.Description?.Value,
            p.Tags.Select(t => t.Value).ToList()
        )).ToList();

        return new GetOrgPetsResponse(items, total, request.Skip, request.Take);
    }
}
```

- [ ] **Step 6: Build**

```bash
dotnet build src/Services/PetService/PetAdoption.PetService.Application
```

- [ ] **Step 7: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommandHandler.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdateOrgPetCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/DeleteOrgPetCommand.cs src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgPetsQuery.cs
git commit -m "add organization-scoped pet CRUD commands and queries"
```

---

### Task 6.3: Create OrgPetsController

**Files:**
- New: `src/Services/PetService/PetAdoption.PetService.API/Controllers/OrgPetsController.cs`

- [ ] **Step 1: Create OrgPetsController**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId}/pets")]
[Authorize]
[ServiceFilter(typeof(OrgAuthorizationFilter))]
public class OrgPetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrgPetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/organizations/{orgId}/pets?status=Available&tags=friendly,vaccinated&skip=0&take=20
    [HttpGet]
    public async Task<ActionResult<GetOrgPetsResponse>> GetAll(
        Guid orgId,
        [FromQuery] string? status = null,
        [FromQuery] string? tags = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        PetStatus? petStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PetStatus>(status, true, out var parsed))
            petStatus = parsed;

        IEnumerable<string>? tagList = null;
        if (!string.IsNullOrWhiteSpace(tags))
            tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = await _mediator.Send(new GetOrgPetsQuery(orgId, petStatus, skip, take, tagList));
        return Ok(result);
    }

    // POST /api/organizations/{orgId}/pets
    [HttpPost]
    public async Task<ActionResult<CreateOrgPetResponse>> Create(Guid orgId, CreateOrgPetRequest request)
    {
        var result = await _mediator.Send(new CreateOrgPetCommand(
            orgId, request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return CreatedAtAction(nameof(GetAll), new { orgId }, result);
    }

    // PUT /api/organizations/{orgId}/pets/{petId}
    [HttpPut("{petId}")]
    public async Task<ActionResult<UpdateOrgPetResponse>> Update(Guid orgId, Guid petId, UpdateOrgPetRequest request)
    {
        var result = await _mediator.Send(new UpdateOrgPetCommand(
            orgId, petId, request.Name, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return Ok(result);
    }

    // DELETE /api/organizations/{orgId}/pets/{petId}
    [HttpDelete("{petId}")]
    public async Task<ActionResult<DeleteOrgPetResponse>> Delete(Guid orgId, Guid petId)
    {
        var result = await _mediator.Send(new DeleteOrgPetCommand(orgId, petId));
        return Ok(result);
    }
}

public record CreateOrgPetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdateOrgPetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
```

- [ ] **Step 2: Register OrgAuthorizationFilter in DI**

In `src/Services/PetService/PetAdoption.PetService.API/Program.cs`, add after `builder.Services.AddTransient<PetTypeSeeder>();`:

```csharp
builder.Services.AddScoped<OrgAuthorizationFilter>();
```

Add `using PetAdoption.PetService.API.Authorization;` at the top.

- [ ] **Step 3: Add error code mapping for new errors**

In `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`, add to the validation section:

```csharp
PetDomainErrorCode.InvalidPetTag => HttpStatusCode.BadRequest,
```

- [ ] **Step 4: Build entire solution**

```bash
dotnet build PetAdoption.sln
```

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/OrgPetsController.cs src/Services/PetService/PetAdoption.PetService.API/Authorization/OrgAuthorizationFilter.cs src/Services/PetService/PetAdoption.PetService.API/Program.cs src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs
git commit -m "add OrgPetsController with organization-scoped CRUD endpoints"
```

---

## Chunk 7: Integration Tests

### Task 7.1: Integration tests for tags on existing endpoints

**Files:**
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`
- Modify: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreatePetRequestBuilder.cs`

- [ ] **Step 1: Update CreatePetRequestBuilder to support tags**

Add to `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreatePetRequestBuilder.cs`:

```csharp
private List<string>? _tags = null;

public CreatePetRequestBuilder WithTags(params string[] tags)
{
    _tags = tags.ToList();
    return this;
}
```

Update the `Build()` method to include tags.

- [ ] **Step 2: Add tag-related integration tests to PetsControllerTests**

Add a new section to `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetsControllerTests.cs`:

```csharp
// ──────────────────────────────────────────────────────────────
// Tags
// ──────────────────────────────────────────────────────────────

[Fact]
public async Task CreatePet_WithTags_ShouldPersistTags()
{
    // Arrange
    var petTypeId = await SeedPetTypeAsync();
    var request = CreatePetRequestBuilder.Default()
        .WithName("TaggedPet")
        .WithPetTypeId(petTypeId)
        .WithTags("friendly", "vaccinated")
        .Build();

    // Act
    var response = await _client.PostAsJsonAsync("/api/pets", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var created = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
    created.Should().NotBeNull();

    // Verify tags via GET
    var getResponse = await _client.GetAsync($"/api/pets/{created!.Id}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var pet = await getResponse.Content.ReadFromJsonAsync<PetDetailWithTagsDto>();
    pet!.Tags.Should().BeEquivalentTo(new[] { "friendly", "vaccinated" });
}

[Fact]
public async Task GetPets_WithTagFilter_ShouldReturnFilteredResults()
{
    // Arrange
    var petTypeId = await SeedPetTypeAsync();

    await CreatePetWithTagsAsync("Pet1", petTypeId, "friendly", "vaccinated");
    await CreatePetWithTagsAsync("Pet2", petTypeId, "friendly");
    await CreatePetWithTagsAsync("Pet3", petTypeId, "neutered");

    // Act - filter by "friendly"
    var response = await _client.GetAsync("/api/pets?tags=friendly");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<PetsWithTagsResponseDto>();
    result!.Pets.Should().OnlyContain(p => p.Tags.Contains("friendly"));
}

// ──────────────────────────────────────────────────────────────
// Tag Helpers
// ──────────────────────────────────────────────────────────────

private async Task<Guid> CreatePetWithTagsAsync(string name, Guid petTypeId, params string[] tags)
{
    var request = CreatePetRequestBuilder.Default()
        .WithName(name)
        .WithPetTypeId(petTypeId)
        .WithTags(tags)
        .Build();

    var response = await _client.PostAsJsonAsync("/api/pets", request);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var created = await response.Content.ReadFromJsonAsync<CreatePetResponseDto>();
    return created!.Id;
}

// ──────────────────────────────────────────────────────────────
// Response DTOs (private)
// ──────────────────────────────────────────────────────────────

private record PetDetailWithTagsDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
private record PetsWithTagsResponseDto(List<PetDetailWithTagsDto> Pets, long Total, int Skip, int Take);
```

- [ ] **Step 3: Run integration tests**

```bash
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests
```

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/
git commit -m "add integration tests for pet tags on existing endpoints"
```

---

### Task 7.2: Integration tests for OrgPetsController

**Files:**
- New: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/OrgPetsControllerTests.cs`
- New: `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreateOrgPetRequestBuilder.cs`

- [ ] **Step 1: Create CreateOrgPetRequestBuilder**

Create `tests/PetService/PetAdoption.PetService.IntegrationTests/Builders/CreateOrgPetRequestBuilder.cs`:

```csharp
using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreateOrgPetRequestBuilder
{
    private string _name = "OrgPet";
    private Guid _petTypeId = Guid.NewGuid();
    private string? _breed = null;
    private int? _ageMonths = null;
    private string? _description = null;
    private List<string>? _tags = null;

    public CreateOrgPetRequestBuilder WithName(string name) { _name = name; return this; }
    public CreateOrgPetRequestBuilder WithPetTypeId(Guid petTypeId) { _petTypeId = petTypeId; return this; }
    public CreateOrgPetRequestBuilder WithBreed(string breed) { _breed = breed; return this; }
    public CreateOrgPetRequestBuilder WithAgeMonths(int ageMonths) { _ageMonths = ageMonths; return this; }
    public CreateOrgPetRequestBuilder WithDescription(string description) { _description = description; return this; }
    public CreateOrgPetRequestBuilder WithTags(params string[] tags) { _tags = tags.ToList(); return this; }

    public CreateOrgPetRequest Build() => new(_name, _petTypeId, _breed, _ageMonths, _description, _tags);

    public static CreateOrgPetRequestBuilder Default() => new();
}
```

- [ ] **Step 2: Create OrgPetsControllerTests**

Create `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/OrgPetsControllerTests.cs`:

```csharp
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
public class OrgPetsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public OrgPetsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        // GenerateTestToken must be extended to accept additionalClaims
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
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_ShouldCreatePetInOrg()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("OrgBuddy")
            .WithPetTypeId(petTypeId)
            .WithTags("friendly", "vaccinated")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ──────────────────────────────────────────────────────────────
    // List
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ShouldReturnOnlyOrgPets()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("OrgPet1")
            .WithPetTypeId(petTypeId)
            .Build();
        await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Act
        var response = await _client.GetAsync($"/api/organizations/{TestOrgId}/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrgPetsResponseDto>();
        result!.Pets.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Authorization
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithWrongOrg_ShouldReturnForbidden()
    {
        // Arrange
        var wrongOrgId = Guid.NewGuid();
        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithPetTypeId(petTypeId)
            .Build();

        // Act - user's org claim doesn't match route org
        var response = await _client.PostAsJsonAsync($"/api/organizations/{wrongOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutOrgClaims_ShouldReturnForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(role: "User"));

        var petTypeId = await SeedPetTypeAsync();
        var request = CreateOrgPetRequestBuilder.Default()
            .WithPetTypeId(petTypeId)
            .Build();

        // Act
        var response = await client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record OrgPetsResponseDto(List<OrgPetItemDto> Pets, long Total, int Skip, int Take);
    private record OrgPetItemDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
    private record CreatePetTypeResponseDto(Guid Id);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
}
```

**Important:** Extend `PetServiceWebAppFactory.GenerateTestToken()` to accept `additionalClaims`:

```csharp
public static string GenerateTestToken(string userId = "test-user-id", string role = "Admin",
    Dictionary<string, string>? additionalClaims = null)
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("test-secret-key-minimum-32-characters-long-for-testing!"));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(ClaimTypes.Role, role),
        new("userId", userId)
    };
    if (additionalClaims is not null)
    {
        foreach (var (claimType, claimValue) in additionalClaims)
            claims.Add(new Claim(claimType, claimValue));
    }
    var token = new JwtSecurityToken(
        issuer: "PetAdoption.UserService",
        audience: "PetAdoption.Services",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

- [ ] **Step 3: Run all tests**

```bash
dotnet test PetAdoption.sln
```

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/
git commit -m "add integration tests for organization-scoped pet CRUD"
```

---

## Chunk 8: Blazor Frontend Updates

### Task 8.1: Update API models and client

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs`
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`

- [ ] **Step 1: Add Tags to API models**

Update existing records in `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs`:

```csharp
// Update existing records to include Tags
public record PetListItem(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags = null);
public record PetDetails(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags = null);
public record CreatePetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdatePetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);

// Add new org-scoped records
public record CreateOrgPetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdateOrgPetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record OrgPetsResponse(IEnumerable<PetListItem> Pets, long Total, int Skip, int Take);
```

- [ ] **Step 2: Add org-scoped methods to PetApiClient**

Add to `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`:

```csharp
// Organization-scoped pet management
public async Task<OrgPetsResponse?> GetOrgPetsAsync(Guid orgId, string? status = null, string? tags = null, int skip = 0, int take = 20)
{
    var query = $"api/organizations/{orgId}/pets?skip={skip}&take={take}";
    if (status is not null) query += $"&status={status}";
    if (tags is not null) query += $"&tags={tags}";
    return await _http.GetFromJsonAsync<OrgPetsResponse>(query);
}

public Task<HttpResponseMessage> CreateOrgPetAsync(Guid orgId, CreateOrgPetRequest request) =>
    _http.PostAsJsonAsync($"api/organizations/{orgId}/pets", request);

public Task<HttpResponseMessage> UpdateOrgPetAsync(Guid orgId, Guid petId, UpdateOrgPetRequest request) =>
    _http.PutAsJsonAsync($"api/organizations/{orgId}/pets/{petId}", request);

public Task<HttpResponseMessage> DeleteOrgPetAsync(Guid orgId, Guid petId) =>
    _http.DeleteAsync($"api/organizations/{orgId}/pets/{petId}");
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs
git commit -m "add tags and org-scoped methods to Blazor API client"
```

---

### Task 8.2: Update PetFormDialog with tag input

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/PetFormDialog.razor`

- [ ] **Step 1: Add tag input to PetFormDialog**

Add after the Description field in the form:

```razor
<MudText Typo="Typo.subtitle2" Class="mb-2">Tags</MudText>
<div class="d-flex gap-2 mb-2">
    <MudTextField @bind-Value="_newTag" Label="Add tag" Variant="Variant.Outlined"
        OnKeyUp="OnTagKeyUp" Immediate="true" />
    <MudIconButton Icon="@Icons.Material.Filled.Add" Color="Color.Primary"
        OnClick="AddTag" Disabled="string.IsNullOrWhiteSpace(_newTag)" />
</div>
<MudChipSet T="string" Class="mb-4">
    @foreach (var tag in _tagList)
    {
        <MudChip T="string" Value="@tag" Color="Color.Primary" Variant="Variant.Outlined"
            OnClose="() => RemoveTag(tag)">@tag</MudChip>
    }
</MudChipSet>
```

Add to the `@code` block:

```csharp
[Parameter] public List<string>? Tags { get; set; }

private List<string> _tagList = new();
private string _newTag = "";

protected override void OnParametersSet()
{
    if (Tags is not null)
        _tagList = new List<string>(Tags);
}

private void AddTag()
{
    var tag = _newTag.Trim().ToLowerInvariant();
    if (!string.IsNullOrWhiteSpace(tag) && !_tagList.Contains(tag))
        _tagList.Add(tag);
    _newTag = "";
}

private void RemoveTag(string tag) => _tagList.Remove(tag);

private void OnTagKeyUp(KeyboardEventArgs e)
{
    if (e.Key == "Enter") AddTag();
}
```

Update `PetFormResult` to include tags:

```csharp
public record PetFormResult(string Name, Guid? PetTypeId, string? Breed, int? AgeMonths, string? Description, List<string> Tags);
```

Update `Submit()`:

```csharp
private void Submit() => MudDialog.Close(DialogResult.Ok(new PetFormResult(Name, SelectedPetTypeId, Breed, AgeMonths, Description, _tagList)));
```

- [ ] **Step 2: Build the Blazor app**

```bash
dotnet build src/Web/PetAdoption.Web.BlazorApp
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Components/Shared/PetFormDialog.razor
git commit -m "add tag input to pet form dialog"
```

---

### Task 8.3: Create Organization Pet Management page

**Files:**
- New: `src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/ManageOrgPets.razor`

- [ ] **Step 1: Create ManageOrgPets page**

```razor
@page "/org/{OrgId:guid}/pets"
@attribute [Authorize]
@inject PetApiClient PetApi
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<PageTitle>Organization Pets</PageTitle>

<div class="d-flex justify-space-between align-center mb-4">
    <MudText Typo="Typo.h4">Organization Pets</MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add"
        OnClick="OpenCreateDialog">Add Pet</MudButton>
</div>

<MudDataGrid T="PetListItem" ServerData="LoadServerData" @ref="_grid" Hover="true" Dense="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.Type" Title="Type" />
        <PropertyColumn Property="x => x.Breed" Title="Breed" />
        <TemplateColumn Title="Age">
            <CellTemplate>
                @if (context.Item.AgeMonths.HasValue)
                {
                    @FormatAge(context.Item.AgeMonths.Value)
                }
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Tags">
            <CellTemplate>
                @if (context.Item.Tags is { Count: > 0 })
                {
                    <MudChipSet T="string" ReadOnly="true">
                        @foreach (var tag in context.Item.Tags.Take(3))
                        {
                            <MudChip T="string" Size="Size.Small" Variant="Variant.Outlined">@tag</MudChip>
                        }
                        @if (context.Item.Tags.Count > 3)
                        {
                            <MudChip T="string" Size="Size.Small" Variant="Variant.Text">+@(context.Item.Tags.Count - 3)</MudChip>
                        }
                    </MudChipSet>
                }
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Status">
            <CellTemplate>
                <MudChip T="string" Size="Size.Small" Color="@StatusColor(context.Item.Status)">@context.Item.Status</MudChip>
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Actions" StickyRight="true">
            <CellTemplate>
                <div class="d-flex gap-1">
                    <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                        OnClick="@(() => OpenEditDialog(context.Item))" />
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                        OnClick="@(() => DeletePet(context.Item))" />
                </div>
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="PetListItem" />
    </PagerContent>
</MudDataGrid>

@code {
    [Parameter] public Guid OrgId { get; set; }

    private MudDataGrid<PetListItem> _grid = null!;

    private async Task<GridData<PetListItem>> LoadServerData(GridState<PetListItem> state)
    {
        try
        {
            var response = await PetApi.GetOrgPetsAsync(OrgId, skip: state.Page * state.PageSize, take: state.PageSize);
            return new GridData<PetListItem>
            {
                Items = response?.Pets ?? [],
                TotalItems = (int)(response?.Total ?? 0)
            };
        }
        catch
        {
            Snackbar.Add("Failed to load pets", Severity.Error);
            return new GridData<PetListItem> { Items = [], TotalItems = 0 };
        }
    }

    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<PetFormDialog>("Add Pet");
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: PetFormDialog.PetFormResult data } && data.PetTypeId.HasValue)
        {
            try
            {
                var response = await PetApi.CreateOrgPetAsync(OrgId,
                    new CreateOrgPetRequest(data.Name, data.PetTypeId.Value, data.Breed, data.AgeMonths, data.Description, data.Tags));
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Pet created!", Severity.Success);
                    await _grid.ReloadServerData();
                }
                else
                    Snackbar.Add("Failed to create pet", Severity.Error);
            }
            catch { Snackbar.Add("Connection error", Severity.Error); }
        }
    }

    private async Task OpenEditDialog(PetListItem pet)
    {
        var parameters = new DialogParameters<PetFormDialog>
        {
            { x => x.IsEdit, true },
            { x => x.Name, pet.Name },
            { x => x.Breed, pet.Breed },
            { x => x.AgeMonths, pet.AgeMonths },
            { x => x.Description, pet.Description },
            { x => x.Tags, pet.Tags?.ToList() }
        };
        var dialog = await DialogService.ShowAsync<PetFormDialog>("Edit Pet", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: PetFormDialog.PetFormResult data })
        {
            try
            {
                var response = await PetApi.UpdateOrgPetAsync(OrgId, pet.Id,
                    new UpdateOrgPetRequest(data.Name, data.Breed, data.AgeMonths, data.Description, data.Tags));
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Pet updated!", Severity.Success);
                    await _grid.ReloadServerData();
                }
                else
                    Snackbar.Add("Failed to update pet", Severity.Error);
            }
            catch { Snackbar.Add("Connection error", Severity.Error); }
        }
    }

    private async Task DeletePet(PetListItem pet)
    {
        bool? confirmed = await DialogService.ShowMessageBox(
            "Confirm Delete", $"Delete {pet.Name}? This cannot be undone.", yesText: "Delete", cancelText: "Cancel");
        if (confirmed == true)
        {
            try
            {
                var response = await PetApi.DeleteOrgPetAsync(OrgId, pet.Id);
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Pet deleted", Severity.Info);
                    await _grid.ReloadServerData();
                }
                else
                    Snackbar.Add("Failed to delete", Severity.Error);
            }
            catch { Snackbar.Add("Connection error", Severity.Error); }
        }
    }

    private static Color StatusColor(string status) => status switch
    {
        "Available" => Color.Success,
        "Reserved" => Color.Warning,
        "Adopted" => Color.Info,
        _ => Color.Default
    };

    private static string FormatAge(int months) =>
        months >= 12 ? $"{months / 12}y {months % 12}m" : $"{months}m";
}
```

- [ ] **Step 2: Build Blazor app**

```bash
dotnet build src/Web/PetAdoption.Web.BlazorApp
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/Organization/ManageOrgPets.razor
git commit -m "add organization pet management page with tag support"
```

---

## Chunk 9: Final Validation

### Task 9.1: Full build and test suite

- [ ] **Step 1: Build entire solution**

```bash
dotnet build PetAdoption.sln
```

- [ ] **Step 2: Run all tests**

```bash
dotnet test PetAdoption.sln
```

- [ ] **Step 3: Verify via Aspire (manual)**

```bash
dotnet run --project src/Aspire/PetAdoption.AppHost
```

Test manually:
1. Create a pet with tags via Swagger at `http://localhost:8080/swagger`
2. GET `/api/pets?tags=friendly` to verify tag filtering
3. Test org-scoped endpoints if Organization Management is in place

---

## Key Design Decisions

1. **JSON column for tags** instead of a separate `PetTags` join table. Simpler for a small tag set (typical pets have 2-10 tags). Trade-off: tag filtering uses in-memory filtering until EF Core improves JSON column query support.

2. **Separate OrgPetsController** rather than adding org-scoped routes to PetsController. Cleanly separates authorization concerns.

3. **Action filter for org auth** rather than policy-based approach. Action filters can read route parameters directly.

4. **AND logic for tag filtering** (`GET /api/pets?tags=friendly,vaccinated` returns pets with BOTH tags). More useful for narrowing search results.

5. **Tags are lowercase-normalized** in the PetTag value object. Prevents duplicates caused by case differences.

6. **PetFormDialog reused** for both admin and org pet management, with Tags parameter added.
