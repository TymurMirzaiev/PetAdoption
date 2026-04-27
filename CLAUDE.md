# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## Build & Run

```bash
dotnet build PetAdoption.sln
dotnet test PetAdoption.sln
docker compose up                          # all services + infra
docker compose up mongo rabbitmq           # infra only
```

## Project Structure

```
PetAdoption/
├── src/Services/
│   ├── PetService/     (.NET 9.0)  — Pet lifecycle management
│   └── UserService/    (.NET 10.0) — Auth, users, RBAC
├── tests/
│   ├── PetService/     UnitTests + IntegrationTests
│   └── UserService/    UnitTests + IntegrationTests
└── docs/
```

## Architecture

- **Clean Architecture**: Domain → Application → Infrastructure → API
- **CQRS**: Separate `IRepository` (write) and `IQueryStore` (read)
- **DDD**: Aggregates, value objects, domain events, factory methods
- **Transactional Outbox**: Domain events saved atomically, published by background service
- **MongoDB** with custom value object serializers (use Filter API, not LINQ)
- **RabbitMQ** for async event publishing
- **JWT + RBAC** (UserService)
- **Custom Mediator** (PetService, not MediatR)

## Coding Conventions

### General

- File-scoped namespaces everywhere
- Records for DTOs, commands, queries, and responses
- Factory methods on aggregates (`Pet.Create()`, `User.Register()`)
- Value objects validate in constructors, throw `ArgumentException` or `DomainException`
- One class/record per file, except: response record lives with its handler

### Dependency Rules

```
API → Infrastructure → Application → Domain
Domain has ZERO external dependencies
Application references only Domain
Never reverse the dependency flow
```

### MongoDB

**Always use Filter API for queries with value objects** (LINQ fails at runtime):
```csharp
// WRONG
await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

// CORRECT
var filter = Builders<User>.Filter.Eq("Email", email.Value);
await _users.Find(filter).FirstOrDefaultAsync();
```

### Error Handling

- Domain exceptions caught by `ExceptionHandlingMiddleware` in both services
- Exception type → HTTP status mapping in middleware
- Never let domain exceptions leak as 500s

## Testing Conventions

### Test Method Naming

```
[MethodUnderTest]_[Scenario]_[ExpectedResult]
```

Examples: `Reserve_WhenAvailable_ShouldChangeStatusToReserved`, `CreatePet_WithEmptyName_ReturnsBadRequest`

### Test Structure (AAA Pattern)

Every test method MUST have `// Arrange`, `// Act`, `// Assert` comments:

```csharp
[Fact]
public async Task Register_WithValidData_ReturnsSuccess()
{
    // Arrange
    var request = RegisterUserRequestBuilder.Default()
        .WithEmail("test@example.com")
        .Build();

    // Act
    var response = await _client.PostAsJsonAsync("/api/users/register", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

For simple single-expression tests (e.g., value object validation with `[Theory]`), `// Act & Assert` is acceptable:

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
public void Constructor_WithEmptyName_ShouldThrow(string name)
{
    // Act & Assert
    var act = () => new PetName(name);
    act.Should().Throw<DomainException>();
}
```

### Section Separators

Group related tests with section separator comments in ALL test files:

```csharp
// ──────────────────────────────────────────────────────────────
// Reserve
// ──────────────────────────────────────────────────────────────
```

Use for: endpoint groups, entity methods, value object operations, helper sections, response DTOs.

### Fluent Builders

Use fluent builders for test object construction (arrange block):

```csharp
public class XBuilder
{
    private string _field = "default";

    public XBuilder WithField(string value) { _field = value; return this; }
    public X Build() => /* construct */;
    public static XBuilder Default() => new();
}
```

### Integration Tests

- `WebApplicationFactory<Program>` + Testcontainers MongoDB
- `[Collection("MongoDB")]` + `IAsyncLifetime` for shared container
- Unique database name per test class for isolation
- Disable RabbitMQ background services in factory
- Private response record DTOs at bottom of test class
- Helper methods for common setup (seed data, create entities, authenticate)

### Unit Tests

- Moq for mocking dependencies in handler tests
- FluentAssertions for all assertions (`.Should()`)
- `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized
- Domain tests: test via public API of aggregate/value object, never test internals

### Assertion Style

- Always use FluentAssertions, never xUnit Assert
- `response.StatusCode.Should().Be(HttpStatusCode.OK)`
- `result.Should().NotBeNull()`
- `pets.Should().HaveCount(3)`
- `act.Should().ThrowAsync<DomainException>()`

## Git

- Never add `Co-Authored-By` lines to commits
- Commit messages: lowercase, imperative, concise

## Per-Service Docs

See `CLAUDE.md` in each service directory for service-specific guidance.
