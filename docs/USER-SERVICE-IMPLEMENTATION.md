# UserService - Implementation Guide

## Overview
UserService is a **Layer 0** service with **no dependencies** on other business services. It can be implemented immediately.

**Purpose:** Manage user accounts, profiles, and preferences for the PetAdoption platform.

**Complexity:** Low-Medium (similar to PetService, good starting point)

**Estimated Effort:** 5-8 days

---

## What We're Building

### Core Features (MVP)
1. ✅ User Registration (email + name)
2. ✅ User Profile Management (update name, phone, preferences)
3. ✅ User Lookup (by ID, by email)
4. ✅ User Status Management (active/suspended)
5. ✅ Event Publishing (user registered, profile updated)

### What We're NOT Building (Yet)
- ❌ Authentication/Login (use external auth provider like Auth0, or add later)
- ❌ Password management (delegate to auth provider)
- ❌ Email verification
- ❌ User roles/permissions (add in Phase 2 if needed)
- ❌ User deletion (add if required)

---

## Project Structure

```
src/Services/UserService/
├── PetAdoption.UserService.API/
│   ├── Controllers/
│   │   └── UsersController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Dockerfile
├── PetAdoption.UserService.Application/
│   ├── Commands/
│   │   ├── RegisterUserCommand.cs
│   │   ├── RegisterUserCommandHandler.cs
│   │   ├── UpdateUserProfileCommand.cs
│   │   ├── UpdateUserProfileCommandHandler.cs
│   │   ├── SuspendUserCommand.cs
│   │   └── SuspendUserCommandHandler.cs
│   ├── Queries/
│   │   ├── GetUserByIdQuery.cs
│   │   ├── GetUserByIdQueryHandler.cs
│   │   ├── GetUserByEmailQuery.cs
│   │   ├── GetUserByEmailQueryHandler.cs
│   │   ├── GetUsersQuery.cs
│   │   └── GetUsersQueryHandler.cs
│   ├── DTOs/
│   │   ├── UserDto.cs
│   │   ├── UserListItemDto.cs
│   │   └── RegisterUserResponse.cs
│   └── Abstractions/
│       ├── ICommand.cs
│       ├── ICommandHandler.cs
│       ├── IQuery.cs
│       └── IQueryHandler.cs
├── PetAdoption.UserService.Domain/
│   ├── Entities/
│   │   ├── User.cs (Aggregate Root)
│   │   └── OutboxEvent.cs
│   ├── ValueObjects/
│   │   ├── UserId.cs
│   │   ├── Email.cs
│   │   ├── FullName.cs
│   │   ├── PhoneNumber.cs
│   │   └── UserPreferences.cs
│   ├── Enums/
│   │   └── UserStatus.cs
│   ├── Events/
│   │   ├── DomainEventBase.cs
│   │   ├── UserRegisteredEvent.cs
│   │   ├── UserProfileUpdatedEvent.cs
│   │   └── UserSuspendedEvent.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   ├── UserNotFoundException.cs
│   │   └── DuplicateEmailException.cs
│   └── Interfaces/
│       ├── IUserRepository.cs
│       ├── IUserQueryStore.cs
│       └── IOutboxRepository.cs
└── PetAdoption.UserService.Infrastructure/
    ├── Persistence/
    │   ├── UserRepository.cs
    │   ├── UserQueryStore.cs
    │   └── OutboxRepository.cs
    ├── Messaging/
    │   ├── RabbitMqOptions.cs
    │   ├── RabbitMqTopologyBuilder.cs
    │   ├── RabbitMqTopologySetup.cs
    │   └── RabbitMqEventPublisher.cs
    ├── BackgroundServices/
    │   └── OutboxProcessorService.cs
    └── DependencyInjection/
        └── ServiceCollectionExtensions.cs
```

---

## Implementation Checklist

## 1. Project Setup

### 1.1 Create Solution Structure
```bash
cd src/Services
mkdir UserService
cd UserService

# Create solution
dotnet new sln -n PetAdoption.UserService

# Create projects
dotnet new webapi -n PetAdoption.UserService.API
dotnet new classlib -n PetAdoption.UserService.Application
dotnet new classlib -n PetAdoption.UserService.Domain
dotnet new classlib -n PetAdoption.UserService.Infrastructure

# Add projects to solution
dotnet sln add PetAdoption.UserService.API/PetAdoption.UserService.API.csproj
dotnet sln add PetAdoption.UserService.Application/PetAdoption.UserService.Application.csproj
dotnet sln add PetAdoption.UserService.Domain/PetAdoption.UserService.Domain.csproj
dotnet sln add PetAdoption.UserService.Infrastructure/PetAdoption.UserService.Infrastructure.csproj

# Add project references
dotnet add PetAdoption.UserService.API reference PetAdoption.UserService.Application
dotnet add PetAdoption.UserService.API reference PetAdoption.UserService.Infrastructure
dotnet add PetAdoption.UserService.Application reference PetAdoption.UserService.Domain
dotnet add PetAdoption.UserService.Infrastructure reference PetAdoption.UserService.Domain
dotnet add PetAdoption.UserService.Infrastructure reference PetAdoption.UserService.Application

# Add to main solution
cd ../../../
dotnet sln PetAdoption.sln add src/Services/UserService/**/*.csproj
```

**TODO 1.1:** Create project structure ✅ Copy these commands

### 1.2 Add NuGet Packages

**Domain Project:**
- No external dependencies (Clean Architecture principle)

**Application Project:**
```bash
cd src/Services/UserService/PetAdoption.UserService.Application
# No packages needed yet (using simple interfaces)
```

**Infrastructure Project:**
```bash
cd src/Services/UserService/PetAdoption.UserService.Infrastructure
dotnet add package MongoDB.Driver
dotnet add package RabbitMQ.Client
dotnet add package Microsoft.Extensions.Options
dotnet add package Microsoft.Extensions.Hosting
```

**API Project:**
```bash
cd src/Services/UserService/PetAdoption.UserService.API
dotnet add package MongoDB.Driver
dotnet add package Swashbuckle.AspNetCore
```

**TODO 1.2:** Add NuGet packages to projects ✅

---

## 2. Domain Layer (Zero Dependencies)

### 2.1 Value Objects

**TODO 2.1.1:** Create `UserId.cs`
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record UserId
{
    public string Value { get; init; }

    private UserId(string value) => Value = value;

    public static UserId Create() => new(Guid.NewGuid().ToString());
    public static UserId From(string value) => new(value);

    public override string ToString() => Value;
}
```

**TODO 2.1.2:** Create `Email.cs` with validation
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record Email
{
    public string Value { get; init; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var trimmed = value.Trim().ToLowerInvariant();

        // Basic email validation
        if (!trimmed.Contains('@') || !trimmed.Contains('.'))
            throw new ArgumentException("Invalid email format", nameof(value));

        if (trimmed.Length > 255)
            throw new ArgumentException("Email cannot exceed 255 characters", nameof(value));

        return new Email(trimmed);
    }

    public override string ToString() => Value;
}
```

**TODO 2.1.3:** Create `FullName.cs`
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record FullName
{
    public string Value { get; init; }

    private FullName(string value) => Value = value;

    public static FullName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty", nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length < 2)
            throw new ArgumentException("Name must be at least 2 characters", nameof(value));

        if (trimmed.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters", nameof(value));

        return new FullName(trimmed);
    }

    public override string ToString() => Value;
}
```

**TODO 2.1.4:** Create `PhoneNumber.cs` (optional)
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record PhoneNumber
{
    public string Value { get; init; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber? FromOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        // Remove common formatting characters
        var cleaned = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (cleaned.Length < 10 || cleaned.Length > 15)
            throw new ArgumentException("Invalid phone number length", nameof(value));

        return new PhoneNumber(cleaned);
    }

    public override string ToString() => Value;
}
```

**TODO 2.1.5:** Create `UserPreferences.cs`
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record UserPreferences
{
    public string? PreferredPetType { get; init; } // Dog, Cat, etc.
    public List<string>? PreferredSizes { get; init; } // Small, Medium, Large
    public string? PreferredAgeRange { get; init; } // Young, Adult, Senior
    public bool ReceiveEmailNotifications { get; init; } = true;
    public bool ReceiveSmsNotifications { get; init; } = false;

    public static UserPreferences Default() => new()
    {
        ReceiveEmailNotifications = true,
        ReceiveSmsNotifications = false
    };
}
```

### 2.2 Enums

**TODO 2.2.1:** Create `UserStatus.cs`
```csharp
namespace PetAdoption.UserService.Domain.Enums;

public enum UserStatus
{
    Active = 0,
    Suspended = 1
}
```

### 2.3 Domain Events

**TODO 2.3.1:** Create `DomainEventBase.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public abstract record DomainEventBase
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

**TODO 2.3.2:** Create `UserRegisteredEvent.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    string FullName,
    DateTime RegisteredAt
) : DomainEventBase;
```

**TODO 2.3.3:** Create `UserProfileUpdatedEvent.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserProfileUpdatedEvent(
    string UserId,
    string? NewFullName,
    string? NewPhoneNumber,
    DateTime UpdatedAt
) : DomainEventBase;
```

**TODO 2.3.4:** Create `UserSuspendedEvent.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserSuspendedEvent(
    string UserId,
    string Reason,
    DateTime SuspendedAt
) : DomainEventBase;
```

### 2.4 User Aggregate Root

**TODO 2.4.1:** Create `User.cs` aggregate
```csharp
namespace PetAdoption.UserService.Domain.Entities;

using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Events;

public class User
{
    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public FullName FullName { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public UserPreferences Preferences { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<DomainEventBase> _domainEvents = new();
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF/MongoDB
    private User() { }

    // Factory method for creating new user
    public static User Register(string email, string fullName, string? phoneNumber = null)
    {
        var user = new User
        {
            Id = UserId.Create(),
            Email = Email.From(email),
            FullName = FullName.From(fullName),
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber),
            Preferences = UserPreferences.Default(),
            Status = UserStatus.Active,
            RegisteredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id.Value,
            user.Email.Value,
            user.FullName.Value,
            user.RegisteredAt
        ));

        return user;
    }

    public void UpdateProfile(string? fullName = null, string? phoneNumber = null, UserPreferences? preferences = null)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("Cannot update profile of suspended user");

        var hasChanges = false;

        if (fullName != null)
        {
            FullName = FullName.From(fullName);
            hasChanges = true;
        }

        if (phoneNumber != null)
        {
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber);
            hasChanges = true;
        }

        if (preferences != null)
        {
            Preferences = preferences;
            hasChanges = true;
        }

        if (hasChanges)
        {
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserProfileUpdatedEvent(
                Id.Value,
                fullName,
                phoneNumber,
                UpdatedAt
            ));
        }
    }

    public void Suspend(string reason)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("User is already suspended");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserSuspendedEvent(
            Id.Value,
            reason,
            UpdatedAt
        ));
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(DomainEventBase @event)
    {
        _domainEvents.Add(@event);
    }
}
```

### 2.5 Exceptions

**TODO 2.5.1:** Create `DomainException.cs`
```csharp
namespace PetAdoption.UserService.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
```

**TODO 2.5.2:** Create `UserNotFoundException.cs`
```csharp
namespace PetAdoption.UserService.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string userId)
        : base("USER_NOT_FOUND", $"User with ID '{userId}' not found")
    {
    }
}
```

**TODO 2.5.3:** Create `DuplicateEmailException.cs`
```csharp
namespace PetAdoption.UserService.Domain.Exceptions;

public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email)
        : base("DUPLICATE_EMAIL", $"User with email '{email}' already exists")
    {
    }
}
```

### 2.6 Repository Interfaces

**TODO 2.6.1:** Create `IUserRepository.cs`
```csharp
namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.ValueObjects;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByEmailAsync(Email email);
    Task<bool> ExistsWithEmailAsync(Email email);
    Task SaveAsync(User user);
}
```

**TODO 2.6.2:** Create `IUserQueryStore.cs` (CQRS - read model)
```csharp
namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.ValueObjects;

public interface IUserQueryStore
{
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByEmailAsync(Email email);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 50);
    Task<int> CountAsync();
}
```

**TODO 2.6.3:** Create `OutboxEvent.cs` entity
```csharp
namespace PetAdoption.UserService.Domain.Entities;

public class OutboxEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
```

**TODO 2.6.4:** Create `IOutboxRepository.cs`
```csharp
namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize = 100);
    Task MarkAsProcessedAsync(string id);
    Task MarkAsFailedAsync(string id, string error);
}
```

---

## 3. Application Layer

### 3.1 Abstractions (Simple Mediator Pattern)

**TODO 3.1.1:** Create `ICommand.cs`
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface ICommand<TResponse>
{
}
```

**TODO 3.1.2:** Create `ICommandHandler.cs`
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

**TODO 3.1.3:** Create `IQuery.cs`
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface IQuery<TResponse>
{
}
```

**TODO 3.1.4:** Create `IQueryHandler.cs`
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

### 3.2 DTOs

**TODO 3.2.1:** Create `UserDto.cs`
```csharp
namespace PetAdoption.UserService.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Status,
    UserPreferencesDto Preferences,
    DateTime RegisteredAt,
    DateTime UpdatedAt
);

public record UserPreferencesDto(
    string? PreferredPetType,
    List<string>? PreferredSizes,
    string? PreferredAgeRange,
    bool ReceiveEmailNotifications,
    bool ReceiveSmsNotifications
);
```

**TODO 3.2.2:** Create `UserListItemDto.cs`
```csharp
namespace PetAdoption.UserService.Application.DTOs;

public record UserListItemDto(
    string Id,
    string Email,
    string FullName,
    string Status,
    DateTime RegisteredAt
);
```

**TODO 3.2.3:** Create `RegisterUserResponse.cs`
```csharp
namespace PetAdoption.UserService.Application.DTOs;

public record RegisterUserResponse(
    bool Success,
    string UserId,
    string Message
);
```

### 3.3 Commands

**TODO 3.3.1:** Create `RegisterUserCommand.cs`
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record RegisterUserCommand(
    string Email,
    string FullName,
    string? PhoneNumber = null
) : ICommand<RegisterUserResponse>;
```

**TODO 3.3.2:** Create `RegisterUserCommandHandler.cs`
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<RegisterUserResponse> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var email = Email.From(command.Email);
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
        {
            throw new DuplicateEmailException(command.Email);
        }

        // Create new user
        var user = User.Register(
            command.Email,
            command.FullName,
            command.PhoneNumber
        );

        // Save user (will publish events via outbox)
        await _userRepository.SaveAsync(user);

        return new RegisterUserResponse(
            Success: true,
            UserId: user.Id.Value,
            Message: "User registered successfully"
        );
    }
}
```

**TODO 3.3.3:** Create `UpdateUserProfileCommand.cs` and handler (similar pattern)

**TODO 3.3.4:** Create `SuspendUserCommand.cs` and handler (similar pattern)

### 3.4 Queries

**TODO 3.4.1:** Create `GetUserByIdQuery.cs` and handler
**TODO 3.4.2:** Create `GetUserByEmailQuery.cs` and handler
**TODO 3.4.3:** Create `GetUsersQuery.cs` and handler (with pagination)

---

## 4. Infrastructure Layer

### 4.1 Copy RabbitMQ Infrastructure from PetService

**TODO 4.1.1:** Copy these files from PetService:
- `RabbitMqOptions.cs`
- `RabbitMqTopologyBuilder.cs`
- `RabbitMqTopologySetup.cs`
- `RabbitMqEventPublisher.cs`

**TODO 4.1.2:** Update namespaces to `PetAdoption.UserService.Infrastructure.Messaging`

### 4.2 Implement Repositories

**TODO 4.2.1:** Create `UserRepository.cs` with MongoDB and Outbox pattern
**TODO 4.2.2:** Create `UserQueryStore.cs` for read operations
**TODO 4.2.3:** Create `OutboxRepository.cs`

### 4.3 Background Services

**TODO 4.3.1:** Create `OutboxProcessorService.cs` (copy from PetService, adjust namespace)

### 4.4 Dependency Injection

**TODO 4.4.1:** Create `ServiceCollectionExtensions.cs` to register all dependencies

---

## 5. API Layer

### 5.1 Controller

**TODO 5.1.1:** Create `UsersController.cs` with these endpoints:
- `POST /api/users` - Register user
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users` - List users (paginated)
- `PUT /api/users/{id}` - Update user profile
- `POST /api/users/{id}/suspend` - Suspend user

### 5.2 Configuration

**TODO 5.2.1:** Create `appsettings.json`
```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://root:example@localhost:27017"
  },
  "Database": {
    "Name": "UserDb"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Exchanges": [
      {
        "Name": "user.events",
        "Type": "topic",
        "Durable": true
      }
    ],
    "Queues": []
  }
}
```

**TODO 5.2.2:** Create `Program.cs` with DI configuration

---

## 6. Testing

**TODO 6.1:** Create unit tests for User aggregate
**TODO 6.2:** Create unit tests for command handlers
**TODO 6.3:** Create integration tests for API endpoints

---

## 7. Docker Setup

**TODO 7.1:** Create Dockerfile
**TODO 7.2:** Add to compose.yaml (port 8081)

---

## Priority Order (What to Build First)

### Phase 1: Core Domain (Day 1-2)
1. ✅ Project setup
2. ✅ Value objects (Email, FullName, UserId)
3. ✅ User aggregate (Register method only)
4. ✅ UserRegisteredEvent
5. ✅ Repository interfaces

### Phase 2: Basic Registration (Day 2-3)
6. ✅ RegisterUserCommand + Handler
7. ✅ UserRepository (MongoDB)
8. ✅ OutboxRepository
9. ✅ RabbitMQ infrastructure (copy from PetService)
10. ✅ UsersController (POST endpoint only)

### Phase 3: Query Operations (Day 3-4)
11. ✅ UserQueryStore
12. ✅ GetUserByIdQuery + Handler
13. ✅ GetUserByEmailQuery + Handler
14. ✅ UsersController (GET endpoints)

### Phase 4: Profile Updates (Day 4-5)
15. ✅ UpdateProfile method on User aggregate
16. ✅ UpdateUserProfileCommand + Handler
17. ✅ UsersController (PUT endpoint)

### Phase 5: Outbox & Events (Day 5-6)
18. ✅ OutboxProcessorService
19. ✅ Event publishing integration
20. ✅ Verify events in RabbitMQ

### Phase 6: Testing & Docker (Day 6-8)
21. ✅ Unit tests
22. ✅ Integration tests
23. ✅ Dockerfile
24. ✅ Docker Compose integration

---

## Definition of Done

UserService is **complete** when:

- ✅ User can register with email + name
- ✅ User profile can be updated
- ✅ User can be queried by ID or email
- ✅ Users can be listed (paginated)
- ✅ Events are published to `user.events` exchange
- ✅ Outbox pattern ensures reliable event delivery
- ✅ API is documented in Swagger
- ✅ Unit tests pass (80%+ coverage)
- ✅ Integration tests pass
- ✅ Service runs in Docker on port 8081
- ✅ Service connects to MongoDB and RabbitMQ
- ✅ Health check endpoint works

---

## Next Steps After UserService

Once UserService is complete:
1. ✅ Test integration with RabbitMQ (view published events)
2. ✅ Verify MongoDB collections created correctly
3. ✅ Document any learnings/issues
4. ➡️ **Move to ChallengeService** (will call UserService API)

---

## Quick Start Commands

```bash
# Create projects
# (run commands from section 1.1)

# Run locally
dotnet run --project src/Services/UserService/PetAdoption.UserService.API

# Test registration
curl -X POST http://localhost:8081/api/users \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","fullName":"John Doe"}'

# Run tests
dotnet test src/Services/UserService/

# Run in Docker
docker compose up userservice
```

---

## Estimated Effort Breakdown

| Task | Estimated Time |
|------|----------------|
| Project setup + packages | 1 hour |
| Domain layer (value objects + User aggregate) | 4-6 hours |
| Application layer (commands/queries) | 6-8 hours |
| Infrastructure (repositories + RabbitMQ) | 8-10 hours |
| API layer (controller + Program.cs) | 4-6 hours |
| Testing (unit + integration) | 8-12 hours |
| Docker setup + debugging | 4-6 hours |
| **Total** | **35-49 hours (5-7 days)** |

---

**Last Updated:** 2026-02-20
**Status:** Ready for Implementation
**Next Milestone:** UserService MVP Complete
