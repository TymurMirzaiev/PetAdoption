# PetService Backend Enhancements Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend PetService with new domain fields (breed, age, description), JWT authentication, CORS, Favorites aggregate, and Announcements aggregate.

**Architecture:** Follows existing Clean Architecture with CQRS, custom mediator, transactional outbox, and MongoDB Filter API. New aggregates (Favorite, Announcement) follow the same patterns as Pet. JWT auth uses shared secret with UserService.

**Tech Stack:** .NET 9.0, MongoDB, RabbitMQ, xUnit, FluentAssertions, Testcontainers

**Spec:** `docs/superpowers/specs/2026-04-29-blazor-ui-design.md`

---

## Chunk 1: Pet Domain Model Extensions

### Task 1: Add PetBreed Value Object

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetBreed.cs`
- Test: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetBreedTests.cs`

- [ ] **Step 1: Write failing tests for PetBreed**

```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class PetBreedTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidBreed_ShouldCreateInstance()
    {
        // Arrange
        var breed = "Golden Retriever";

        // Act
        var result = new PetBreed(breed);

        // Assert
        result.Value.Should().Be("Golden Retriever");
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var result = new PetBreed("  Labrador  ");

        // Assert
        result.Value.Should().Be("Labrador");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? breed)
    {
        // Act & Assert
        var act = () => new PetBreed(breed!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithTooLongBreed_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetBreed(new string('a', 101));
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var breed1 = new PetBreed("Siamese");
        var breed2 = new PetBreed("Siamese");

        // Act & Assert
        breed1.Should().Be(breed2);
    }

    // ──────────────────────────────────────────────────────────────
    // Conversion
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var breed = new PetBreed("Poodle");

        // Act
        string result = breed;

        // Assert
        result.Should().Be("Poodle");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~PetBreedTests" --no-build 2>&1 | head -5`
Expected: Build failure — `PetBreed` does not exist

- [ ] **Step 3: Implement PetBreed**

```csharp
namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetBreed : IEquatable<PetBreed>
{
    public string Value { get; }

    public PetBreed(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidPetBreed, "Breed cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
            throw new DomainException(PetDomainErrorCode.InvalidPetBreed, "Breed cannot exceed 100 characters.");

        Value = trimmed;
    }

    public bool Equals(PetBreed? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PetBreed other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(PetBreed? left, PetBreed? right) => Equals(left, right);
    public static bool operator !=(PetBreed? left, PetBreed? right) => !Equals(left, right);

    public static implicit operator string(PetBreed breed) => breed.Value;
    public static explicit operator PetBreed(string value) => new(value);
}
```

- [ ] **Step 4: Add error code constant**

Add to `src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs`:

```csharp
public const string InvalidPetBreed = "invalid_pet_breed";
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~PetBreedTests"`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetBreed.cs src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetBreedTests.cs
git commit -m "add PetBreed value object with validation and tests"
```

---

### Task 2: Add PetAge Value Object

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetAge.cs`
- Test: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetAgeTests.cs`

- [ ] **Step 1: Write failing tests for PetAge**

```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class PetAgeTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidAge_ShouldCreateInstance()
    {
        // Arrange & Act
        var age = new PetAge(24);

        // Assert
        age.Months.Should().Be(24);
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreateInstance()
    {
        // Arrange & Act
        var age = new PetAge(0);

        // Assert
        age.Months.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeAge_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetAge(-1);
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var age1 = new PetAge(12);
        var age2 = new PetAge(12);

        // Act & Assert
        age1.Should().Be(age2);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~PetAgeTests" --no-build 2>&1 | head -5`
Expected: Build failure

- [ ] **Step 3: Implement PetAge**

```csharp
namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetAge : IEquatable<PetAge>
{
    public int Months { get; }

    public PetAge(int months)
    {
        if (months < 0)
            throw new DomainException(PetDomainErrorCode.InvalidPetAge, "Age cannot be negative.");

        Months = months;
    }

    public bool Equals(PetAge? other) => other is not null && Months == other.Months;
    public override bool Equals(object? obj) => obj is PetAge other && Equals(other);
    public override int GetHashCode() => Months.GetHashCode();
    public override string ToString() => $"{Months} months";

    public static bool operator ==(PetAge? left, PetAge? right) => Equals(left, right);
    public static bool operator !=(PetAge? left, PetAge? right) => !Equals(left, right);
}
```

- [ ] **Step 4: Add error code constant**

Add to `PetDomainErrorCode.cs`:

```csharp
public const string InvalidPetAge = "invalid_pet_age";
```

- [ ] **Step 5: Run tests, verify pass**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~PetAgeTests"`

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetAge.cs src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetAgeTests.cs
git commit -m "add PetAge value object with validation and tests"
```

---

### Task 3: Add PetDescription Value Object

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetDescription.cs`
- Test: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetDescriptionTests.cs`

- [ ] **Step 1: Write failing tests for PetDescription**

```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class PetDescriptionTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidDescription_ShouldCreateInstance()
    {
        // Arrange & Act
        var desc = new PetDescription("A friendly dog.");

        // Assert
        desc.Value.Should().Be("A friendly dog.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => new PetDescription(value!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithTooLongDescription_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetDescription(new string('a', 2001));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var desc = new PetDescription("  Hello  ");

        // Assert
        desc.Value.Should().Be("Hello");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Implement PetDescription**

```csharp
namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetDescription : IEquatable<PetDescription>
{
    public string Value { get; }

    public PetDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidPetDescription, "Description cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 2000)
            throw new DomainException(PetDomainErrorCode.InvalidPetDescription, "Description cannot exceed 2000 characters.");

        Value = trimmed;
    }

    public bool Equals(PetDescription? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PetDescription other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(PetDescription? left, PetDescription? right) => Equals(left, right);
    public static bool operator !=(PetDescription? left, PetDescription? right) => !Equals(left, right);

    public static implicit operator string(PetDescription desc) => desc.Value;
}
```

- [ ] **Step 4: Add error code constant**

Add to `PetDomainErrorCode.cs`:

```csharp
public const string InvalidPetDescription = "invalid_pet_description";
```

- [ ] **Step 5: Run tests, verify pass**

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/PetDescription.cs src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/PetDomainErrorCode.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetDescriptionTests.cs
git commit -m "add PetDescription value object with validation and tests"
```

---

### Task 4: Extend Pet Aggregate with New Fields

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`
- Modify: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTests.cs`

- [ ] **Step 1: Write failing tests for extended Pet.Create**

Add to `PetTests.cs` in the Create section:

```csharp
// ──────────────────────────────────────────────────────────────
// Create (extended fields)
// ──────────────────────────────────────────────────────────────

[Fact]
public void Create_WithAllFields_ShouldSetProperties()
{
    // Arrange
    var petTypeId = Guid.NewGuid();

    // Act
    var pet = Pet.Create("Bella", petTypeId, "Golden Retriever", 24, "Friendly dog");

    // Assert
    pet.Name.Value.Should().Be("Bella");
    pet.PetTypeId.Should().Be(petTypeId);
    pet.Breed.Should().NotBeNull();
    pet.Breed!.Value.Should().Be("Golden Retriever");
    pet.Age.Should().NotBeNull();
    pet.Age!.Months.Should().Be(24);
    pet.Description.Should().NotBeNull();
    pet.Description!.Value.Should().Be("Friendly dog");
}

[Fact]
public void Create_WithNullOptionalFields_ShouldLeaveNull()
{
    // Arrange
    var petTypeId = Guid.NewGuid();

    // Act
    var pet = Pet.Create("Bella", petTypeId);

    // Assert
    pet.Breed.Should().BeNull();
    pet.Age.Should().BeNull();
    pet.Description.Should().BeNull();
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Extend Pet aggregate**

Modify `Pet.cs`:

1. Add new properties after `Version`:
```csharp
public PetBreed? Breed { get; private set; }
public PetAge? Age { get; private set; }
public PetDescription? Description { get; private set; }
```

2. Update the existing private constructor to accept new optional fields:
```csharp
private Pet(Guid id, PetName name, Guid petTypeId, PetBreed? breed = null, PetAge? age = null, PetDescription? description = null)
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
}
```

3. Update `Create` factory method (existing 2-arg calls still work via defaults):
```csharp
public static Pet Create(string name, Guid petTypeId, string? breed = null, int? ageMonths = null, string? description = null)
{
    return new Pet(
        Guid.NewGuid(),
        new PetName(name),
        petTypeId,
        breed is not null ? new PetBreed(breed) : null,
        ageMonths.HasValue ? new PetAge(ageMonths.Value) : null,
        description is not null ? new PetDescription(description) : null);
}
```

Note: The `using PetAdoption.PetService.Domain.ValueObjects;` already exists in Pet.cs.

- [ ] **Step 4: Run all Pet domain tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests --filter "FullyQualifiedName~PetTests"`
Expected: All pass (existing tests still use 2-arg Create which uses defaults)

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTests.cs
git commit -m "extend Pet aggregate with breed, age, and description fields"
```

---

### Task 5: Add MongoDB Serializers for New Value Objects

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/MongoDbConfiguration.cs`

- [ ] **Step 1: Add serializers for PetBreed, PetAge, PetDescription**

Follow the existing `PetNameSerializer` pattern in `MongoDbConfiguration.cs`. Add serializer classes and register them.

```csharp
public class PetBreedSerializer : SerializerBase<PetBreed?>
{
    public override PetBreed? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        var value = context.Reader.ReadString();
        return new PetBreed(value);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetBreed? value)
    {
        if (value is null)
            context.Writer.WriteNull();
        else
            context.Writer.WriteString(value.Value);
    }
}

public class PetAgeSerializer : SerializerBase<PetAge?>
{
    public override PetAge? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        var value = context.Reader.ReadInt32();
        return new PetAge(value);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetAge? value)
    {
        if (value is null)
            context.Writer.WriteNull();
        else
            context.Writer.WriteInt32(value.Months);
    }
}

public class PetDescriptionSerializer : SerializerBase<PetDescription?>
{
    public override PetDescription? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        var value = context.Reader.ReadString();
        return new PetDescription(value);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetDescription? value)
    {
        if (value is null)
            context.Writer.WriteNull();
        else
            context.Writer.WriteString(value.Value);
    }
}
```

Register them in the `Configure()` method alongside existing serializers:

```csharp
BsonSerializer.RegisterSerializer(new PetBreedSerializer());
BsonSerializer.RegisterSerializer(new PetAgeSerializer());
BsonSerializer.RegisterSerializer(new PetDescriptionSerializer());
```

- [ ] **Step 2: Run full test suite to verify nothing breaks**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests && dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/MongoDbConfiguration.cs
git commit -m "add MongoDB serializers for PetBreed, PetAge, PetDescription"
```

---

### Task 6: Update DTOs and Commands for New Fields

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/DTOs/PetListItemDto.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommand.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommandHandler.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs` (contains command, response, AND handler in one file)
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetByIdQuery.cs` (handler file with PetDetailsDto)
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetAllPetsQueryHandler.cs`

- [ ] **Step 1: Update PetListItemDto**

```csharp
public record PetListItemDto(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);
```

- [ ] **Step 2: Update PetDetailsDto** (in GetPetByIdQuery handler file)

Add `Breed`, `AgeMonths`, `Description` fields to the existing `PetDetailsDto` record.

- [ ] **Step 3: Update CreatePetCommand**

```csharp
public record CreatePetCommand(string Name, Guid PetTypeId, string? Breed, int? AgeMonths, string? Description) : IRequest<CreatePetResponse>;
```

- [ ] **Step 4: Update CreatePetCommandHandler**

Change the `Pet.Create` call to pass new fields:
```csharp
var pet = Pet.Create(request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description);
```

- [ ] **Step 5: Update UpdatePetCommand and handler**

Add `Breed`, `AgeMonths`, `Description` to the update command record. In the handler, add domain methods to update these fields (see Task 7).

- [ ] **Step 6: Update query handlers to map new fields**

In `GetAllPetsQueryHandler` and `GetPetByIdQueryHandler`, update the DTO mappings to include new fields:
```csharp
Breed = pet.Breed?.Value,
AgeMonths = pet.Age?.Months,
Description = pet.Description?.Value
```

- [ ] **Step 7: Update PetsController request DTOs**

Update `CreatePetRequest` and `UpdatePetRequest` records in the controller to include `Breed`, `AgeMonths`, `Description` fields.

- [ ] **Step 8: Run full test suite**

Run: `dotnet build PetAdoption.sln && dotnet test PetAdoption.sln`
Expected: Some integration tests may need updating due to DTO changes

- [ ] **Step 9: Fix any broken tests**

Update integration test builders (`CreatePetRequestBuilder`, `UpdatePetRequestBuilder`) to include new optional fields. Update assertion DTOs in integration tests.

- [ ] **Step 10: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/ src/Services/PetService/PetAdoption.PetService.API/Controllers/ tests/PetService/
git commit -m "update DTOs, commands, and queries for new pet fields"
```

---

### Task 7: Add Domain Methods for Updating New Fields

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`
- Modify: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// ──────────────────────────────────────────────────────────────
// UpdateBreed
// ──────────────────────────────────────────────────────────────

[Fact]
public void UpdateBreed_WithValidBreed_ShouldUpdate()
{
    // Arrange
    var pet = Pet.Create("Bella", Guid.NewGuid());

    // Act
    pet.UpdateBreed("Labrador");

    // Assert
    pet.Breed!.Value.Should().Be("Labrador");
}

[Fact]
public void UpdateBreed_WithNull_ShouldClearBreed()
{
    // Arrange
    var pet = Pet.Create("Bella", Guid.NewGuid(), breed: "Labrador");

    // Act
    pet.UpdateBreed(null);

    // Assert
    pet.Breed.Should().BeNull();
}

// ──────────────────────────────────────────────────────────────
// UpdateAge
// ──────────────────────────────────────────────────────────────

[Fact]
public void UpdateAge_WithValidAge_ShouldUpdate()
{
    // Arrange
    var pet = Pet.Create("Bella", Guid.NewGuid());

    // Act
    pet.UpdateAge(36);

    // Assert
    pet.Age!.Months.Should().Be(36);
}

// ──────────────────────────────────────────────────────────────
// UpdateDescription
// ──────────────────────────────────────────────────────────────

[Fact]
public void UpdateDescription_WithValidDescription_ShouldUpdate()
{
    // Arrange
    var pet = Pet.Create("Bella", Guid.NewGuid());

    // Act
    pet.UpdateDescription("Very friendly");

    // Assert
    pet.Description!.Value.Should().Be("Very friendly");
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Implement domain methods on Pet**

Add to `Pet.cs`:

```csharp
public void UpdateBreed(string? breed)
{
    Breed = breed is not null ? new PetBreed(breed) : null;
}

public void UpdateAge(int? ageMonths)
{
    Age = ageMonths.HasValue ? new PetAge(ageMonths.Value) : null;
}

public void UpdateDescription(string? description)
{
    Description = description is not null ? new PetDescription(description) : null;
}
```

- [ ] **Step 4: Run tests, verify pass**

- [ ] **Step 5: Wire into UpdatePetCommandHandler**

In the handler, after `UpdateName`, call the new methods:
```csharp
pet.UpdateBreed(request.Breed);
pet.UpdateAge(request.AgeMonths);
pet.UpdateDescription(request.Description);
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test PetAdoption.sln`

- [ ] **Step 7: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/PetTests.cs
git commit -m "add domain methods for updating breed, age, and description"
```

---

## Chunk 2: JWT Authentication & CORS for PetService

### Task 8: Add JWT Authentication to PetService

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/PetAdoption.PetService.API.csproj`
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Program.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.API/appsettings.Development.json`

- [ ] **Step 1: Add JWT NuGet package**

Add to the API `.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.*" />
```

- [ ] **Step 2: Add JWT config to appsettings.Development.json**

```json
"Jwt": {
    "Secret": "your-super-secret-key-minimum-32-characters-long-change-in-production!",
    "Issuer": "PetAdoption.UserService",
    "Audience": "PetAdoption.Services"
}
```

Note: Same issuer/audience as UserService so tokens are cross-compatible.

- [ ] **Step 3: Configure JWT auth in Program.cs**

Add after existing service registrations, before `var app = builder.Build()`:

```csharp
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
```

Add middleware after exception handling, in this exact order:
```csharp
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

Note: CORS must come before auth so preflight requests succeed.

- [ ] **Step 4: Add auth attributes to controllers**

In `PetsController.cs`:
- `[AllowAnonymous]` on `GET /api/pets` and `GET /api/pets/{id}`
- `[Authorize]` on `POST /api/pets/{id}/reserve`, `/adopt`, `/cancel-reservation`
- `[Authorize(Policy = "AdminOnly")]` on `POST /api/pets` (create), `PUT /api/pets/{id}`, `DELETE /api/pets/{id}`

In `PetTypesAdminController.cs`:
- `[Authorize(Policy = "AdminOnly")]` on the controller class

- [ ] **Step 5: Update integration test factory**

In `PetServiceWebAppFactory.cs`, add JWT config to test configuration and a helper to generate test JWT tokens. Note: New repositories (FavoriteRepository, FavoriteQueryStore, AnnouncementRepository, AnnouncementQueryStore) take `IMongoDatabase` in their constructors, so they automatically use the test database registered by the factory — no test doubles needed for them.

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureAppConfiguration((context, config) =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-minimum-32-characters-long-for-testing!",
            ["Jwt:Issuer"] = "PetAdoption.UserService",
            ["Jwt:Audience"] = "PetAdoption.Services"
        });
    });
    // ... existing configuration
}
```

Add test token helper method to test base or factory:
```csharp
public static string GenerateTestToken(string userId = "test-user-id", string role = "Admin")
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("test-secret-key-minimum-32-characters-long-for-testing!"));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(ClaimTypes.Role, role),
        new Claim("userId", userId)
    };
    var token = new JwtSecurityToken(
        issuer: "PetAdoption.UserService",
        audience: "PetAdoption.Services",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

- [ ] **Step 6: Update integration tests to use auth tokens**

Add `Authorization` header to all `HttpClient` calls in integration tests:
```csharp
_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken());
```

- [ ] **Step 7: Run all tests**

Run: `dotnet test PetAdoption.sln`

- [ ] **Step 8: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/ tests/PetService/PetAdoption.PetService.IntegrationTests/
git commit -m "add JWT authentication and authorization to PetService"
```

---

### Task 9: Add CORS to PetService

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Program.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.API/appsettings.Development.json`

- [ ] **Step 1: Add CORS config to appsettings**

```json
"Cors": {
    "AllowedOrigins": ["https://localhost:7100", "http://localhost:5100"]
}
```

- [ ] **Step 2: Configure CORS in Program.cs**

Add before `var app`:
```csharp
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

Note: `app.UseCors()` was already added in Task 8 (before `UseAuthentication`).

- [ ] **Step 3: Run tests**

Run: `dotnet test PetAdoption.sln`

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Program.cs src/Services/PetService/PetAdoption.PetService.API/appsettings.Development.json
git commit -m "add CORS configuration to PetService"
```

---

## Chunk 3: Favorites Aggregate

### Task 10: Favorite Domain Entity

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Favorite.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IFavoriteRepository.cs`
- Create: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/FavoriteTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain;

public class FavoriteTests
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
        var favorite = Favorite.Create(userId, petId);

        // Assert
        favorite.Id.Should().NotBeEmpty();
        favorite.UserId.Should().Be(userId);
        favorite.PetId.Should().Be(petId);
        favorite.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => Favorite.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => Favorite.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Implement Favorite entity**

```csharp
namespace PetAdoption.PetService.Domain;

public class Favorite
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PetId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Favorite() { }

    public static Favorite Create(Guid userId, Guid petId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId cannot be empty.", nameof(petId));

        return new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PetId = petId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 4: Create IFavoriteRepository**

```csharp
namespace PetAdoption.PetService.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task AddAsync(Favorite favorite);
    Task<Favorite?> GetByUserAndPetAsync(Guid userId, Guid petId);
    Task DeleteAsync(Guid userId, Guid petId);
}
```

- [ ] **Step 5: Create IFavoriteQueryStore** in Application layer

Create `src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs`:

```csharp
namespace PetAdoption.PetService.Application.Queries;

public interface IFavoriteQueryStore
{
    Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take);
}

public record FavoriteWithPetDto(
    Guid FavoriteId, Guid PetId, string PetName, string PetType,
    string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
```

- [ ] **Step 6: Run tests, verify pass**

- [ ] **Step 7: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/FavoriteTests.cs
git commit -m "add Favorite domain entity and repository interfaces"
```

---

### Task 11: Favorite MongoDB Repository

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteRepository.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/MongoDbConfiguration.cs`
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Add Favorite class map in MongoDbConfiguration**

```csharp
BsonClassMap.RegisterClassMap<Favorite>(cm =>
{
    cm.AutoMap();
    cm.MapIdMember(c => c.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
    cm.SetIgnoreExtraElements(true);
});
```

- [ ] **Step 2: Implement FavoriteRepository**

```csharp
namespace PetAdoption.PetService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly IMongoCollection<Favorite> _favorites;

    public FavoriteRepository(IMongoDatabase database)
    {
        _favorites = database.GetCollection<Favorite>("Favorites");
        var indexBuilder = Builders<Favorite>.IndexKeys;
        var uniqueIndex = new CreateIndexModel<Favorite>(
            indexBuilder.Ascending("UserId").Ascending("PetId"),
            new CreateIndexOptions { Unique = true });
        _favorites.Indexes.CreateOne(uniqueIndex);
    }

    public async Task AddAsync(Favorite favorite)
    {
        await _favorites.InsertOneAsync(favorite);
    }

    public async Task<Favorite?> GetByUserAndPetAsync(Guid userId, Guid petId)
    {
        var filter = Builders<Favorite>.Filter.And(
            Builders<Favorite>.Filter.Eq("UserId", userId),
            Builders<Favorite>.Filter.Eq("PetId", petId));
        return await _favorites.Find(filter).FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(Guid userId, Guid petId)
    {
        var filter = Builders<Favorite>.Filter.And(
            Builders<Favorite>.Filter.Eq("UserId", userId),
            Builders<Favorite>.Filter.Eq("PetId", petId));
        await _favorites.DeleteOneAsync(filter);
    }
}
```

- [ ] **Step 3: Implement FavoriteQueryStore**

```csharp
namespace PetAdoption.PetService.Infrastructure.Persistence;

using MongoDB.Bson;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

public class FavoriteQueryStore : IFavoriteQueryStore
{
    private readonly IMongoCollection<Favorite> _favorites;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<PetType> _petTypes;

    public FavoriteQueryStore(IMongoDatabase database)
    {
        _favorites = database.GetCollection<Favorite>("Favorites");
        _pets = database.GetCollection<Pet>("Pets");
        _petTypes = database.GetCollection<PetType>("PetTypes");
    }

    public async Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take)
    {
        var filter = Builders<Favorite>.Filter.Eq("UserId", userId);
        var total = await _favorites.CountDocumentsAsync(filter);

        var favorites = await _favorites.Find(filter)
            .SortByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        var petIds = favorites.Select(f => f.PetId).ToList();
        var petFilter = Builders<Pet>.Filter.In("_id", petIds);
        var pets = await _pets.Find(petFilter).ToListAsync();
        var petDict = pets.ToDictionary(p => p.Id);

        var typeIds = pets.Select(p => p.PetTypeId).Distinct().ToList();
        var typeFilter = Builders<PetType>.Filter.In("_id", typeIds);
        var types = await _petTypes.Find(typeFilter).ToListAsync();
        var typeDict = types.ToDictionary(t => t.Id);

        var items = favorites.Select(f =>
        {
            var pet = petDict.GetValueOrDefault(f.PetId);
            var typeName = pet is not null && typeDict.TryGetValue(pet.PetTypeId, out var pt) ? pt.Name : "Unknown";
            return new FavoriteWithPetDto(
                f.Id, f.PetId,
                pet?.Name.Value ?? "Deleted",
                typeName,
                pet?.Breed?.Value,
                pet?.Age?.Months,
                pet?.Status.ToString() ?? "Unknown",
                f.CreatedAt);
        });

        return (items, total);
    }
}
```

- [ ] **Step 4: Register in DI**

Add to `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IFavoriteRepository, FavoriteRepository>();
services.AddScoped<IFavoriteQueryStore, FavoriteQueryStore>();
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/
git commit -m "add Favorite MongoDB repository and query store"
```

---

### Task 12: Favorite Command and Query Handlers

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/AddFavoriteCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/AddFavoriteCommandHandler.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/RemoveFavoriteCommand.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Commands/RemoveFavoriteCommandHandler.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQueryHandler.cs`

- [ ] **Step 1: Create AddFavoriteCommand**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record AddFavoriteCommand(Guid UserId, Guid PetId) : IRequest<AddFavoriteResponse>;
```

- [ ] **Step 2: Create AddFavoriteCommandHandler**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record AddFavoriteResponse(Guid Id, Guid PetId, DateTime CreatedAt);

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, AddFavoriteResponse>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IPetRepository _petRepository;

    public AddFavoriteCommandHandler(IFavoriteRepository favoriteRepository, IPetRepository petRepository)
    {
        _favoriteRepository = favoriteRepository;
        _petRepository = petRepository;
    }

    public async Task<AddFavoriteResponse> Handle(AddFavoriteCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");

        var existing = await _favoriteRepository.GetByUserAndPetAsync(request.UserId, request.PetId);
        if (existing is not null)
            throw new DomainException(PetDomainErrorCode.FavoriteAlreadyExists, "Pet is already in favorites.");

        var favorite = Favorite.Create(request.UserId, request.PetId);
        await _favoriteRepository.AddAsync(favorite);

        return new AddFavoriteResponse(favorite.Id, favorite.PetId, favorite.CreatedAt);
    }
}
```

- [ ] **Step 3: Add new error codes to PetDomainErrorCode**

```csharp
public const string FavoriteAlreadyExists = "favorite_already_exists";
public const string FavoriteNotFound = "favorite_not_found";
```

Add to `ExceptionHandlingMiddleware.cs` mapping:
- `favorite_already_exists` → 409 Conflict
- `favorite_not_found` → 404

- [ ] **Step 4: Create RemoveFavoriteCommand**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record RemoveFavoriteCommand(Guid UserId, Guid PetId) : IRequest<RemoveFavoriteResponse>;
```

- [ ] **Step 5: Create RemoveFavoriteCommandHandler**

```csharp
namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record RemoveFavoriteResponse(bool Success);

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, RemoveFavoriteResponse>
{
    private readonly IFavoriteRepository _favoriteRepository;

    public RemoveFavoriteCommandHandler(IFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task<RemoveFavoriteResponse> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken = default)
    {
        var existing = await _favoriteRepository.GetByUserAndPetAsync(request.UserId, request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.FavoriteNotFound, "Favorite not found.");

        await _favoriteRepository.DeleteAsync(request.UserId, request.PetId);
        return new RemoveFavoriteResponse(true);
    }
}
```

- [ ] **Step 6: Create GetFavoritesQuery**

```csharp
namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;

public record GetFavoritesQuery(Guid UserId, int Skip, int Take) : IRequest<GetFavoritesResponse>;
```

- [ ] **Step 7: Create GetFavoritesQueryHandler**

```csharp
namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;

public record GetFavoritesResponse(IEnumerable<FavoriteWithPetDto> Items, long TotalCount, int Page, int PageSize);

public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, GetFavoritesResponse>
{
    private readonly IFavoriteQueryStore _queryStore;

    public GetFavoritesQueryHandler(IFavoriteQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetFavoritesResponse> Handle(GetFavoritesQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetByUserAsync(request.UserId, request.Skip, request.Take);
        return new GetFavoritesResponse(items, total, request.Skip / request.Take + 1, request.Take);
    }
}
```

- [ ] **Step 8: Build**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 9: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/ src/Services/PetService/PetAdoption.PetService.Domain/Exceptions/ src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/
git commit -m "add Favorite command and query handlers"
```

---

### Task 13: Favorites Controller and Integration Tests

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs`
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/FavoritesControllerTests.cs`

- [ ] **Step 1: Create FavoritesController**

```csharp
namespace PetAdoption.PetService.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var result = await _mediator.Send(new AddFavoriteCommand(GetUserId(), request.PetId));
        return StatusCode(201, result);
    }

    [HttpDelete("{petId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid petId)
    {
        await _mediator.Send(new RemoveFavoriteCommand(GetUserId(), petId));
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var result = await _mediator.Send(new GetFavoritesQuery(GetUserId(), skip, take));
        return Ok(result);
    }
}

public record AddFavoriteRequest(Guid PetId);
```

- [ ] **Step 2: Write integration tests**

```csharp
namespace PetAdoption.PetService.IntegrationTests.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Infrastructure;

[Collection("MongoDB")]
public class FavoritesControllerTests : IAsyncLifetime
{
    private readonly PetServiceWebAppFactory _factory;
    private readonly HttpClient _client;

    public FavoritesControllerTests(MongoDbFixture mongoDbFixture)
    {
        _factory = new PetServiceWebAppFactory(mongoDbFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken());
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreatePetAsync()
    {
        var petTypeId = await SeedPetTypeAsync();
        var response = await _client.PostAsJsonAsync("/api/pets", new { Name = "TestPet", PetTypeId = petTypeId });
        var body = await response.Content.ReadFromJsonAsync<CreatePetResponse>();
        return body!.Id;
    }

    private async Task<Guid> SeedPetTypeAsync()
    {
        var response = await _client.GetFromJsonAsync<PetTypeListResponse>("/api/admin/pet-types");
        return response!.Items.First().Id;
    }

    // ──────────────────────────────────────────────────────────────
    // Add Favorite
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddFavorite_WithValidPet_ReturnsCreated()
    {
        // Arrange
        var petId = await CreatePetAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsConflict()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────
    // Remove Favorite
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFavorite_Existing_ReturnsNoContent()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.DeleteAsync($"/api/favorites/{petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ──────────────────────────────────────────────────────────────
    // Get Favorites
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFavorites_WithFavorites_ReturnsList()
    {
        // Arrange
        var petId = await CreatePetAsync();
        await _client.PostAsJsonAsync("/api/favorites", new { PetId = petId });

        // Act
        var response = await _client.GetAsync("/api/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FavoritesResponse>();
        body!.Items.Should().HaveCountGreaterThan(0);
    }

    // ──────────────────────────────────────────────────────────────
    // Auth
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddFavorite_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/favorites", new { PetId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreatePetResponse(Guid Id);
    private record PetTypeListResponse(IEnumerable<PetTypeItem> Items);
    private record PetTypeItem(Guid Id, string Code, string Name);
    private record FavoritesResponse(IEnumerable<FavoriteItem> Items, long TotalCount);
    private record FavoriteItem(Guid FavoriteId, Guid PetId, string PetName);
}
```

- [ ] **Step 3: Run integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~FavoritesControllerTests"`

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/FavoritesControllerTests.cs
git commit -m "add Favorites controller and integration tests"
```

---

## Chunk 4: Announcements Aggregate

### Task 14: Announcement Domain Entity and Value Objects

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Announcement.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/AnnouncementTitle.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/ValueObjects/AnnouncementBody.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/IAnnouncementRepository.cs`
- Create: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AnnouncementTests.cs`
- Create: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AnnouncementTitleTests.cs`
- Create: `tests/PetService/PetAdoption.PetService.UnitTests/Domain/AnnouncementBodyTests.cs`

- [ ] **Step 1: Write value object tests**

`AnnouncementTitleTests.cs`:
```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class AnnouncementTitleTests
{
    [Fact]
    public void Constructor_WithValidTitle_ShouldCreateInstance()
    {
        // Act
        var title = new AnnouncementTitle("Holiday Hours");

        // Assert
        title.Value.Should().Be("Holiday Hours");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => new AnnouncementTitle(value!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_ExceedingMaxLength_ShouldThrow()
    {
        // Act & Assert
        var act = () => new AnnouncementTitle(new string('a', 201));
        act.Should().Throw<DomainException>();
    }
}
```

`AnnouncementBodyTests.cs` — similar pattern, max 5000 chars.

`AnnouncementTests.cs`:
```csharp
namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

public class AnnouncementTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddDays(7);
        var createdBy = Guid.NewGuid();

        // Act
        var announcement = Announcement.Create("Title", "Body text", start, end, createdBy);

        // Assert
        announcement.Id.Should().NotBeEmpty();
        announcement.Title.Value.Should().Be("Title");
        announcement.Body.Value.Should().Be("Body text");
        announcement.StartDate.Should().Be(start);
        announcement.EndDate.Should().Be(end);
        announcement.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Create_WithEndBeforeStart_ShouldThrow()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);

        // Act & Assert
        var act = () => Announcement.Create("Title", "Body", start, end, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var announcement = Announcement.Create("Old", "Old body", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), Guid.NewGuid());
        var newEnd = DateTime.UtcNow.AddDays(14);

        // Act
        announcement.Update("New", "New body", DateTime.UtcNow, newEnd);

        // Assert
        announcement.Title.Value.Should().Be("New");
        announcement.Body.Value.Should().Be("New body");
    }
}
```

- [ ] **Step 2: Implement value objects**

`AnnouncementTitle.cs`:
```csharp
namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class AnnouncementTitle : IEquatable<AnnouncementTitle>
{
    public string Value { get; }

    public AnnouncementTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementTitle, "Title cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > 200)
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementTitle, "Title cannot exceed 200 characters.");
        Value = trimmed;
    }

    public bool Equals(AnnouncementTitle? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is AnnouncementTitle other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
    public static implicit operator string(AnnouncementTitle t) => t.Value;
}
```

`AnnouncementBody.cs` — same pattern, max 5000 chars, error code `InvalidAnnouncementBody`.

- [ ] **Step 3: Implement Announcement entity**

```csharp
namespace PetAdoption.PetService.Domain;

using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

public class Announcement
{
    public Guid Id { get; private set; }
    public AnnouncementTitle Title { get; private set; } = null!;
    public AnnouncementBody Body { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Announcement() { }

    public static Announcement Create(string title, string body, DateTime startDate, DateTime endDate, Guid createdBy)
    {
        ValidateDates(startDate, endDate);

        return new Announcement
        {
            Id = Guid.NewGuid(),
            Title = new AnnouncementTitle(title),
            Body = new AnnouncementBody(body),
            StartDate = startDate,
            EndDate = endDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string body, DateTime startDate, DateTime endDate)
    {
        ValidateDates(startDate, endDate);
        Title = new AnnouncementTitle(title);
        Body = new AnnouncementBody(body);
        StartDate = startDate;
        EndDate = endDate;
    }

    private static void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementDates, "End date must be after start date.");
    }
}
```

- [ ] **Step 4: Add error codes**

Add to `PetDomainErrorCode.cs`:
```csharp
public const string InvalidAnnouncementTitle = "invalid_announcement_title";
public const string InvalidAnnouncementBody = "invalid_announcement_body";
public const string InvalidAnnouncementDates = "invalid_announcement_dates";
public const string AnnouncementNotFound = "announcement_not_found";
```

- [ ] **Step 5: Create IAnnouncementRepository**

```csharp
namespace PetAdoption.PetService.Domain.Interfaces;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(Guid id);
    Task AddAsync(Announcement announcement);
    Task UpdateAsync(Announcement announcement);
    Task DeleteAsync(Guid id);
}
```

- [ ] **Step 6: Create IAnnouncementQueryStore** in Application layer

```csharp
namespace PetAdoption.PetService.Application.Queries;

public interface IAnnouncementQueryStore
{
    Task<(IEnumerable<AnnouncementListDto> Items, long Total)> GetAllAsync(int skip, int take);
    Task<AnnouncementDetailDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ActiveAnnouncementDto>> GetActiveAsync();
}

public record AnnouncementListDto(Guid Id, string Title, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
public record AnnouncementDetailDto(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy, DateTime CreatedAt);
public record ActiveAnnouncementDto(Guid Id, string Title, string Body);
```

- [ ] **Step 7: Run tests, verify pass**

- [ ] **Step 8: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Domain/ src/Services/PetService/PetAdoption.PetService.Application/Queries/IAnnouncementQueryStore.cs tests/PetService/PetAdoption.PetService.UnitTests/Domain/Announcement*
git commit -m "add Announcement domain entity, value objects, and tests"
```

---

### Task 15: Announcement Infrastructure and API

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AnnouncementRepository.cs`
- Create: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/AnnouncementQueryStore.cs`
- Create: command/query handlers (6 files in Application layer)
- Create: `src/Services/PetService/PetAdoption.PetService.API/Controllers/AnnouncementsController.cs`
- Modify: `MongoDbConfiguration.cs`, `ServiceCollectionExtensions.cs`, `ExceptionHandlingMiddleware.cs`

- [ ] **Step 1: Add MongoDB class map and serializers**

Register `Announcement` class map and serializers for `AnnouncementTitle`, `AnnouncementBody` in `MongoDbConfiguration.cs`.

- [ ] **Step 2: Implement AnnouncementRepository**

Follow `PetTypeRepository` pattern. Collection: `Announcements`. Standard CRUD with Filter API.

- [ ] **Step 3: Implement AnnouncementQueryStore**

`GetActiveAsync`: filter where `StartDate <= now && EndDate >= now`.
`GetAllAsync`: paginated, compute status from dates.
`GetByIdAsync`: single lookup.

- [ ] **Step 4: Create command handlers**

- `CreateAnnouncementCommand(Title, Body, StartDate, EndDate, CreatedBy)` + handler
- `UpdateAnnouncementCommand(Id, Title, Body, StartDate, EndDate)` + handler
- `DeleteAnnouncementCommand(Id)` + handler

Follow existing command handler patterns (IRequest/IRequestHandler).

- [ ] **Step 5: Create query handlers**

- `GetAnnouncementsQuery(Skip, Take)` + handler
- `GetAnnouncementByIdQuery(Id)` + handler
- `GetActiveAnnouncementsQuery` + handler

- [ ] **Step 6: Create AnnouncementsController**

```csharp
namespace PetAdoption.PetService.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;

[ApiController]
[Route("api/announcements")]
public class AnnouncementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnnouncementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue("userId")!);
        var result = await _mediator.Send(new CreateAnnouncementCommand(
            request.Title, request.Body, request.StartDate, request.EndDate, userId));
        return StatusCode(201, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementRequest request)
    {
        var result = await _mediator.Send(new UpdateAnnouncementCommand(
            id, request.Title, request.Body, request.StartDate, request.EndDate));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAnnouncementCommand(id));
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var result = await _mediator.Send(new GetAnnouncementsQuery(skip, take));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetAnnouncementByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveAnnouncementsQuery());
        return Ok(result);
    }
}

public record CreateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
public record UpdateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
```

- [ ] **Step 7: Update ExceptionHandlingMiddleware**

Add new error codes to the mapping:
```csharp
"announcement_not_found" => HttpStatusCode.NotFound,
"invalid_announcement_title" => HttpStatusCode.BadRequest,
"invalid_announcement_body" => HttpStatusCode.BadRequest,
"invalid_announcement_dates" => HttpStatusCode.BadRequest,
```

- [ ] **Step 8: Register in DI**

Add repositories and query stores to `ServiceCollectionExtensions.cs`.

- [ ] **Step 9: Run build**

Run: `dotnet build PetAdoption.sln`

- [ ] **Step 10: Commit**

```bash
git add src/Services/PetService/ tests/PetService/
git commit -m "add Announcements CRUD with controller, handlers, and infrastructure"
```

---

### Task 16: Announcements Integration Tests

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/AnnouncementsControllerTests.cs`

- [ ] **Step 1: Write integration tests**

Test CRUD operations, auth enforcement, active announcements query, date validation. Follow existing test patterns (`[Collection("MongoDB")]`, `IAsyncLifetime`, private response DTOs, helpers).

Key tests:
- `CreateAnnouncement_WithValidData_ReturnsCreated`
- `CreateAnnouncement_WithEndBeforeStart_ReturnsBadRequest`
- `CreateAnnouncement_WithoutAdminRole_ReturnsForbidden`
- `UpdateAnnouncement_Existing_ReturnsOk`
- `DeleteAnnouncement_Existing_ReturnsNoContent`
- `GetActive_ReturnsOnlyCurrentAnnouncements`
- `GetActive_WithoutAuth_ReturnsOk`

- [ ] **Step 2: Run integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~AnnouncementsControllerTests"`

- [ ] **Step 3: Run full test suite**

Run: `dotnet test PetAdoption.sln`

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/AnnouncementsControllerTests.cs
git commit -m "add Announcements integration tests"
```

---

## Chunk 5: Final Verification

### Task 17: Full Build and Test Verification

- [ ] **Step 1: Clean build**

Run: `dotnet clean PetAdoption.sln && dotnet build PetAdoption.sln`

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`

- [ ] **Step 3: Run all integration tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests`

- [ ] **Step 4: Run UserService tests to verify no regressions**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 5: Update PetService CLAUDE.md**

Update `src/Services/PetService/CLAUDE.md` to document:
- New domain fields (Breed, Age, Description)
- JWT authentication on PetService
- Favorites aggregate and endpoints
- Announcements aggregate and endpoints
- New error codes

- [ ] **Step 6: Commit**

```bash
git add src/Services/PetService/CLAUDE.md
git commit -m "update PetService documentation with new features"
```
