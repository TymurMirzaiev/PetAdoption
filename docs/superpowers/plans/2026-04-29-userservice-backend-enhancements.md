# UserService Backend Enhancements Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add refresh tokens, Google SSO, activate user endpoint, and CORS to UserService.

**Architecture:** Follows existing UserService patterns — ICommandHandler/IQueryHandler (no mediator), MongoDB with Filter API, BCrypt password hashing, JWT via JwtTokenGenerator. New features extend existing domain and infrastructure.

**Tech Stack:** .NET 10.0, MongoDB, RabbitMQ, xUnit, FluentAssertions, Moq

**Spec:** `docs/superpowers/specs/2026-04-29-blazor-ui-design.md`

**Depends on:** Nothing (independent of PetService plan)

---

## Chunk 1: Refresh Tokens

### Task 1: RefreshToken Domain Entity

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Entities/RefreshToken.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IRefreshTokenRepository.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Domain/RefreshTokenTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
namespace PetAdoption.UserService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;

public class RefreshTokenTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var token = RefreshToken.Create(userId, TimeSpan.FromDays(30));

        // Assert
        token.Id.Should().NotBeNullOrEmpty();
        token.UserId.Should().Be("user-123");
        token.Token.Should().NotBeNullOrEmpty();
        token.Token.Length.Should().BeGreaterThanOrEqualTo(32);
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        token.IsRevoked.Should().BeFalse();
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithEmptyUserId_ShouldThrow(string? userId)
    {
        // Act & Assert
        var act = () => RefreshToken.Create(userId!, TimeSpan.FromDays(30));
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Revoke
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_ShouldSetIsRevokedTrue()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // IsValid
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));

        // Act & Assert
        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        token.Revoke();

        // Act & Assert
        token.IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "FullyQualifiedName~RefreshTokenTests" --no-build 2>&1 | head -5`

- [ ] **Step 3: Implement RefreshToken entity**

```csharp
namespace PetAdoption.UserService.Domain.Entities;

using System.Security.Cryptography;

public class RefreshToken
{
    public string Id { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(string userId, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
```

- [ ] **Step 4: Create IRefreshTokenRepository**

```csharp
namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAllForUserAsync(string userId);
}
```

- [ ] **Step 5: Run tests, verify pass**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "FullyQualifiedName~RefreshTokenTests"`

- [ ] **Step 6: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Entities/RefreshToken.cs src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IRefreshTokenRepository.cs tests/UserService/PetAdoption.UserService.UnitTests/Domain/RefreshTokenTests.cs
git commit -m "add RefreshToken domain entity and repository interface"
```

---

### Task 2: RefreshToken MongoDB Repository

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/RefreshTokenRepository.cs`
- Modify: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/MongoDbConfiguration.cs`
- Modify: `src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Add RefreshToken class map in MongoDbConfiguration.cs**

Add inside the `if (!_configured)` block, alongside existing class maps:

```csharp
BsonClassMap.RegisterClassMap<RefreshToken>(cm =>
{
    cm.AutoMap();
    cm.MapIdMember(c => c.Id);
    cm.SetIgnoreExtraElements(true);
});
```

- [ ] **Step 2: Implement RefreshTokenRepository**

```csharp
namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _tokens;

    public RefreshTokenRepository(IMongoDatabase database)
    {
        _tokens = database.GetCollection<RefreshToken>("RefreshTokens");

        var indexBuilder = Builders<RefreshToken>.IndexKeys;
        _tokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            indexBuilder.Ascending("Token"),
            new CreateIndexOptions { Unique = true }));
        _tokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            indexBuilder.Ascending("UserId")));
    }

    public async Task SaveAsync(RefreshToken refreshToken)
    {
        var filter = Builders<RefreshToken>.Filter.Eq("_id", refreshToken.Id);
        await _tokens.ReplaceOneAsync(filter, refreshToken, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        var filter = Builders<RefreshToken>.Filter.Eq("Token", token);
        return await _tokens.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var filter = Builders<RefreshToken>.Filter.And(
            Builders<RefreshToken>.Filter.Eq("UserId", userId),
            Builders<RefreshToken>.Filter.Eq("IsRevoked", false));
        var update = Builders<RefreshToken>.Update.Set("IsRevoked", true);
        await _tokens.UpdateManyAsync(filter, update);
    }
}
```

- [ ] **Step 3: Register in DI**

Add to `ServiceCollectionExtensions.cs` in the Repositories section:
```csharp
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
```

- [ ] **Step 4: Build to verify compilation**

Run: `dotnet build src/Services/UserService/PetAdoption.UserService.API/PetAdoption.UserService.API.csproj`

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/RefreshTokenRepository.cs src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/MongoDbConfiguration.cs src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
git commit -m "add RefreshToken MongoDB repository with indexes"
```

---

### Task 3: Refresh Token Command Handler

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/RefreshTokenCommand.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/RefreshTokenCommandHandler.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/RefreshTokenCommandHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Enums;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _handler = new RefreshTokenCommandHandler(
            _mockRefreshTokenRepo.Object, _mockUserRepo.Object, _mockJwtGenerator.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldReturnNewTokenPair()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        var user = CreateTestUser("user-123");

        _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);
        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new-access-token");

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(existingToken.Token);
    }

    // ──────────────────────────────────────────────────────────────
    // Errors
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithInvalidToken_ShouldThrow()
    {
        // Arrange
        _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync("bad-token"))
            .ReturnsAsync((RefreshToken?)null);

        var command = new RefreshTokenCommand("bad-token");

        // Act & Assert
        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_WithRevokedToken_ShouldThrow()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        existingToken.Revoke();

        _mockRefreshTokenRepo.Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act & Assert
        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static User CreateTestUser(string userId)
    {
        var user = User.Register("test@example.com", "Test User", "$2a$12$hashedpassword");
        // Use reflection to set the Id since it's set internally
        typeof(User).GetProperty("Id")!.SetValue(user, UserId.From(userId));
        return user;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create RefreshTokenCommand**

```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken);
```

- [ ] **Step 4: Create RefreshTokenCommandHandler**

```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        IJwtTokenGenerator jwtGenerator)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<RefreshTokenResponse> HandleAsync(
        RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _refreshTokenRepo.GetByTokenAsync(command.RefreshToken);
        if (existing is null || !existing.IsValid)
            throw new InvalidCredentialsException();

        existing.Revoke();
        await _refreshTokenRepo.SaveAsync(existing);

        var user = await _userRepo.GetByIdAsync(UserId.From(existing.UserId))
            ?? throw new UserNotFoundException(existing.UserId);

        var accessToken = _jwtGenerator.GenerateToken(
            user.Id.Value, user.Email.Value, user.Role.ToString());

        var newRefreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(30));
        await _refreshTokenRepo.SaveAsync(newRefreshToken);

        return new RefreshTokenResponse(accessToken, newRefreshToken.Token);
    }
}
```

- [ ] **Step 5: Register handler in DI**

Add to `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>, RefreshTokenCommandHandler>();
```

- [ ] **Step 6: Run tests, verify pass**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "FullyQualifiedName~RefreshTokenCommandHandlerTests"`

- [ ] **Step 7: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Application/Commands/RefreshTokenCommand.cs src/Services/UserService/PetAdoption.UserService.Application/Commands/RefreshTokenCommandHandler.cs src/Services/UserService/PetAdoption.UserService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/RefreshTokenCommandHandlerTests.cs
git commit -m "add refresh token command handler with tests"
```

---

### Task 4: Update Login to Return Refresh Token

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.Application/Commands/LoginCommandHandler.cs`
- Modify: `src/Services/UserService/PetAdoption.UserService.Application/DTOs/LoginResponse.cs` (or wherever LoginResponse is defined)
- Modify: `tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/LoginCommandHandlerTests.cs`

- [ ] **Step 1: Update LoginResponse to include RefreshToken**

Add `RefreshToken` field to the existing `LoginResponse` record:
```csharp
public record LoginResponse(
    bool Success, string Token, string RefreshToken,
    string UserId, string Email, string FullName, string Role, int ExpiresIn);
```

- [ ] **Step 2: Update LoginCommandHandler**

Inject `IRefreshTokenRepository` into the handler constructor. After generating the JWT, create and save a refresh token:

```csharp
var refreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(30));
await _refreshTokenRepo.SaveAsync(refreshToken);

return new LoginResponse(
    Success: true,
    Token: token,
    RefreshToken: refreshToken.Token,
    UserId: user.Id.Value,
    Email: user.Email.Value,
    FullName: user.FullName.Value,
    Role: user.Role.ToString(),
    ExpiresIn: 3600);
```

- [ ] **Step 3: Update LoginCommandHandlerTests**

Update existing tests to mock `IRefreshTokenRepository`. Update assertions to check `RefreshToken` field is populated.

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests --filter "FullyQualifiedName~LoginCommandHandlerTests"`

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Application/Commands/ src/Services/UserService/PetAdoption.UserService.Application/DTOs/ tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/LoginCommandHandlerTests.cs
git commit -m "update login to return refresh token"
```

---

### Task 5: Add Refresh and Logout Endpoints

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.API/Controllers/UsersController.cs`

- [ ] **Step 1: Add refresh endpoint**

```csharp
[HttpPost("refresh")]
[AllowAnonymous]
public async Task<IActionResult> RefreshToken(
    [FromBody] RefreshTokenRequest request,
    [FromServices] ICommandHandler<RefreshTokenCommand, RefreshTokenResponse> handler)
{
    var command = new RefreshTokenCommand(request.RefreshToken);
    var response = await handler.HandleAsync(command);
    return Ok(response);
}
```

- [ ] **Step 2: Add logout endpoint**

```csharp
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout(
    [FromBody] LogoutRequest request,
    [FromServices] IRefreshTokenRepository refreshTokenRepo)
{
    var token = await refreshTokenRepo.GetByTokenAsync(request.RefreshToken);
    if (token is not null)
    {
        token.Revoke();
        await refreshTokenRepo.SaveAsync(token);
    }
    return NoContent();
}
```

- [ ] **Step 3: Add request DTOs at bottom of controller**

```csharp
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
```

- [ ] **Step 4: Build and run tests**

Run: `dotnet build PetAdoption.sln && dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.API/Controllers/UsersController.cs
git commit -m "add refresh token and logout endpoints"
```

---

## Chunk 2: Google SSO

### Task 6: Add ExternalProvider to User Domain

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.Domain/Entities/User.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Domain/UserGoogleSsoTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
namespace PetAdoption.UserService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;

public class UserGoogleSsoTests
{
    // ──────────────────────────────────────────────────────────────
    // RegisterFromGoogle
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterFromGoogle_WithValidData_ShouldCreateUser()
    {
        // Arrange & Act
        var user = User.RegisterFromGoogle("test@gmail.com", "Test User");

        // Assert
        user.Email.Value.Should().Be("test@gmail.com");
        user.FullName.Value.Should().Be("Test User");
        user.ExternalProvider.Should().Be("Google");
        user.Password.Should().BeNull();
    }

    [Fact]
    public void RegisterFromGoogle_ShouldHaveActiveStatus()
    {
        // Act
        var user = User.RegisterFromGoogle("test@gmail.com", "Test User");

        // Assert
        user.Status.Should().Be(Domain.Enums.UserStatus.Active);
        user.Role.Should().Be(Domain.Enums.UserRole.User);
    }

    // ──────────────────────────────────────────────────────────────
    // HasPassword
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void HasPassword_ForGoogleUser_ShouldBeFalse()
    {
        // Arrange
        var user = User.RegisterFromGoogle("test@gmail.com", "Test");

        // Act & Assert
        user.HasPassword.Should().BeFalse();
    }

    [Fact]
    public void HasPassword_ForRegularUser_ShouldBeTrue()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test", "$2a$12$hash");

        // Act & Assert
        user.HasPassword.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Extend User aggregate**

Add property to `User.cs`:
```csharp
public string? ExternalProvider { get; private set; }
public bool HasPassword => Password is not null;
```

Make Password nullable:
```csharp
public Password? Password { get; private set; }
```

Add new factory method:
```csharp
public static User RegisterFromGoogle(string email, string fullName)
{
    var user = new User
    {
        Id = UserId.Create(),
        Email = Email.From(email),
        FullName = FullName.From(fullName),
        Password = null,
        Role = UserRole.User,
        PhoneNumber = null,
        Preferences = UserPreferences.Default(),
        Status = UserStatus.Active,
        ExternalProvider = "Google",
        RegisteredAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        LastLoginAt = null
    };

    user.AddDomainEvent(new UserRegisteredEvent(
        user.Id.Value, user.Email.Value, user.FullName.Value,
        user.Role.ToString(), user.RegisteredAt));

    return user;
}
```

- [ ] **Step 4: Update ChangePassword to check HasPassword**

In `ChangePassword` method, add guard:
```csharp
if (!HasPassword)
    throw new InvalidOperationException("Cannot change password for SSO user");
```

- [ ] **Step 5: Run tests, verify pass**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 6: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.Domain/Entities/User.cs tests/UserService/PetAdoption.UserService.UnitTests/Domain/UserGoogleSsoTests.cs
git commit -m "add ExternalProvider field and RegisterFromGoogle factory method"
```

---

### Task 7: Google Auth Command Handler

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/GoogleAuthCommand.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/GoogleAuthCommandHandler.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Abstractions/IGoogleTokenValidator.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Infrastructure/Security/GoogleTokenValidator.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/GoogleAuthCommandHandlerTests.cs`

- [ ] **Step 1: Create IGoogleTokenValidator abstraction**

```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken);
}

public record GoogleUserInfo(string Email, string FullName);
```

- [ ] **Step 2: Write failing tests**

```csharp
namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class GoogleAuthCommandHandlerTests
{
    private readonly Mock<IGoogleTokenValidator> _mockGoogleValidator;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly GoogleAuthCommandHandler _handler;

    public GoogleAuthCommandHandlerTests()
    {
        _mockGoogleValidator = new Mock<IGoogleTokenValidator>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _handler = new GoogleAuthCommandHandler(
            _mockGoogleValidator.Object, _mockUserRepo.Object,
            _mockJwtGenerator.Object, _mockRefreshTokenRepo.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Existing user
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ExistingGoogleUser_ShouldReturnTokens()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("test@gmail.com", "Test User");
        _mockGoogleValidator.Setup(v => v.ValidateAsync("valid-token")).ReturnsAsync(googleInfo);

        var existingUser = User.RegisterFromGoogle("test@gmail.com", "Test User");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>())).ReturnsAsync(existingUser);
        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        var result = await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        _mockUserRepo.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // New user (auto-register)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NewGoogleUser_ShouldAutoRegister()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("new@gmail.com", "New User");
        _mockGoogleValidator.Setup(v => v.ValidateAsync("valid-token")).ReturnsAsync(googleInfo);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>())).ReturnsAsync((User?)null);
        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        var result = await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        _mockUserRepo.Verify(r => r.SaveAsync(It.Is<User>(u => u.ExternalProvider == "Google")), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid token
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_InvalidGoogleToken_ShouldThrow()
    {
        // Arrange
        _mockGoogleValidator.Setup(v => v.ValidateAsync("bad-token")).ReturnsAsync((GoogleUserInfo?)null);

        // Act & Assert
        var act = () => _handler.HandleAsync(new GoogleAuthCommand("bad-token"));
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}
```

- [ ] **Step 3: Create GoogleAuthCommand**

```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record GoogleAuthCommand(string IdToken) : ICommand<GoogleAuthResponse>;

public record GoogleAuthResponse(string AccessToken, string RefreshToken);
```

- [ ] **Step 4: Create GoogleAuthCommandHandler**

```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class GoogleAuthCommandHandler : ICommandHandler<GoogleAuthCommand, GoogleAuthResponse>
{
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public GoogleAuthCommandHandler(
        IGoogleTokenValidator googleValidator,
        IUserRepository userRepo,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepo)
    {
        _googleValidator = googleValidator;
        _userRepo = userRepo;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<GoogleAuthResponse> HandleAsync(
        GoogleAuthCommand command, CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleValidator.ValidateAsync(command.IdToken)
            ?? throw new InvalidCredentialsException();

        var email = Email.From(googleUser.Email);
        var user = await _userRepo.GetByEmailAsync(email);

        if (user is null)
        {
            user = User.RegisterFromGoogle(googleUser.Email, googleUser.FullName);
            await _userRepo.SaveAsync(user);
        }
        else
        {
            if (user.Status == Domain.Enums.UserStatus.Suspended)
                throw new UserSuspendedException(user.Id.Value);

            user.RecordLogin();
            await _userRepo.SaveAsync(user);
        }

        var accessToken = _jwtGenerator.GenerateToken(
            user.Id.Value, user.Email.Value, user.Role.ToString());

        var refreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(30));
        await _refreshTokenRepo.SaveAsync(refreshToken);

        return new GoogleAuthResponse(accessToken, refreshToken.Token);
    }
}
```

- [ ] **Step 5: Implement GoogleTokenValidator**

```csharp
namespace PetAdoption.UserService.Infrastructure.Security;

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PetAdoption.UserService.Application.Abstractions;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;

    public GoogleTokenValidator(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured");
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<GoogleTokenInfo>(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}");

            if (response is null || response.Aud != _clientId)
                return null;

            if (string.IsNullOrEmpty(response.Email) || response.EmailVerified != "true")
                return null;

            return new GoogleUserInfo(response.Email, response.Name ?? response.Email);
        }
        catch
        {
            return null;
        }
    }

    private record GoogleTokenInfo(string? Email, string? EmailVerified, string? Name, string? Aud);
}
```

- [ ] **Step 6: Register in DI**

Add to `ServiceCollectionExtensions.cs`:
```csharp
services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();
services.AddScoped<ICommandHandler<GoogleAuthCommand, GoogleAuthResponse>, GoogleAuthCommandHandler>();
```

- [ ] **Step 7: Add Google config to appsettings**

Add to `appsettings.json`:
```json
"Google": {
    "ClientId": "your-google-client-id.apps.googleusercontent.com"
}
```

- [ ] **Step 8: Run tests, verify pass**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 9: Commit**

```bash
git add src/Services/UserService/ tests/UserService/
git commit -m "add Google SSO authentication with auto-registration"
```

---

### Task 8: Google Auth Endpoint

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.API/Controllers/UsersController.cs`

- [ ] **Step 1: Add Google auth endpoint**

```csharp
[HttpPost("auth/google")]
[AllowAnonymous]
public async Task<IActionResult> GoogleAuth(
    [FromBody] GoogleAuthRequest request,
    [FromServices] ICommandHandler<GoogleAuthCommand, GoogleAuthResponse> handler)
{
    var command = new GoogleAuthCommand(request.IdToken);
    var response = await handler.HandleAsync(command);
    return Ok(response);
}
```

Add request DTO:
```csharp
public record GoogleAuthRequest(string IdToken);
```

- [ ] **Step 2: Update UserDto to include ExternalProvider and HasPassword**

Add to `UserDto`:
```csharp
public record UserDto(
    string Id, string Email, string FullName, string? PhoneNumber,
    string Status, string Role, UserPreferencesDto Preferences,
    string? ExternalProvider, bool HasPassword,
    DateTime RegisteredAt, DateTime UpdatedAt, DateTime? LastLoginAt);
```

Update query handlers that map to UserDto to include the new fields.

- [ ] **Step 3: Build and run tests**

Run: `dotnet build PetAdoption.sln && dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 4: Commit**

```bash
git add src/Services/UserService/
git commit -m "add Google auth endpoint and update UserDto"
```

---

## Chunk 3: Activate Endpoint & CORS

### Task 9: Activate User Command and Endpoint

**Files:**
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/ActivateUserCommand.cs`
- Create: `src/Services/UserService/PetAdoption.UserService.Application/Commands/ActivateUserCommandHandler.cs`
- Create: `tests/UserService/PetAdoption.UserService.UnitTests/Application/Commands/ActivateUserCommandHandlerTests.cs`
- Modify: `src/Services/UserService/PetAdoption.UserService.API/Controllers/UsersController.cs`

- [ ] **Step 1: Write failing tests**

```csharp
namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class ActivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly ActivateUserCommandHandler _handler;

    public ActivateUserCommandHandlerTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _handler = new ActivateUserCommandHandler(_mockUserRepo.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SuspendedUser_ShouldActivate()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test", "$2a$12$hash");
        user.Suspend("test reason");
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(new ActivateUserCommand(user.Id.Value));

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepo.Verify(r => r.SaveAsync(It.Is<User>(u => u.Status == Domain.Enums.UserStatus.Active)), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Errors
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NonExistentUser_ShouldThrow()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync((User?)null);

        // Act & Assert
        var act = () => _handler.HandleAsync(new ActivateUserCommand("non-existent"));
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_AlreadyActiveUser_ShouldThrow()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test", "$2a$12$hash");
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync(user);

        // Act & Assert
        var act = () => _handler.HandleAsync(new ActivateUserCommand(user.Id.Value));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Create command and handler**

`ActivateUserCommand.cs`:
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record ActivateUserCommand(string UserId) : ICommand<ActivateUserResponse>;
```

`ActivateUserCommandHandler.cs`:
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public record ActivateUserResponse(bool Success, string Message);

public class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand, ActivateUserResponse>
{
    private readonly IUserRepository _userRepo;

    public ActivateUserCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ActivateUserResponse> HandleAsync(
        ActivateUserCommand command, CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new UserNotFoundException(command.UserId);

        user.Activate();
        await _userRepo.SaveAsync(user);

        return new ActivateUserResponse(true, "User activated successfully");
    }
}
```

- [ ] **Step 3: Register in DI**

```csharp
services.AddScoped<ICommandHandler<ActivateUserCommand, ActivateUserResponse>, ActivateUserCommandHandler>();
```

- [ ] **Step 4: Add endpoint to controller**

```csharp
[HttpPost("{id}/activate")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> ActivateUser(
    string id,
    [FromServices] ICommandHandler<ActivateUserCommand, ActivateUserResponse> handler)
{
    var command = new ActivateUserCommand(id);
    var response = await handler.HandleAsync(command);
    return Ok(response);
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 6: Commit**

```bash
git add src/Services/UserService/ tests/UserService/
git commit -m "add activate user endpoint for admin"
```

---

### Task 10: Add CORS to UserService

**Files:**
- Modify: `src/Services/UserService/PetAdoption.UserService.API/Program.cs`
- Modify: `src/Services/UserService/PetAdoption.UserService.API/appsettings.json`

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

Add in the middleware pipeline, before `UseAuthentication`:
```csharp
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build PetAdoption.sln && dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 4: Commit**

```bash
git add src/Services/UserService/PetAdoption.UserService.API/Program.cs src/Services/UserService/PetAdoption.UserService.API/appsettings.json
git commit -m "add CORS configuration to UserService"
```

---

## Chunk 4: Verification

### Task 11: Full Build and Test

- [ ] **Step 1: Clean build**

Run: `dotnet clean PetAdoption.sln && dotnet build PetAdoption.sln`

- [ ] **Step 2: Run all UserService tests**

Run: `dotnet test tests/UserService/PetAdoption.UserService.UnitTests`

- [ ] **Step 3: Run PetService tests (no regressions)**

Run: `dotnet test tests/PetService/PetAdoption.PetService.UnitTests`

- [ ] **Step 4: Update UserService docs**

Update `src/Services/UserService/README.md` or relevant docs to document:
- Refresh token endpoints (`POST /api/users/refresh`, `POST /api/users/logout`)
- Google SSO endpoint (`POST /api/users/auth/google`)
- Activate user endpoint (`POST /api/users/{id}/activate`)
- Updated login response with refresh token
- CORS configuration

- [ ] **Step 5: Commit**

```bash
git add src/Services/UserService/
git commit -m "update UserService documentation with new features"
```
