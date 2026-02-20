# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build PetAdoption.sln

# Run the UserService API via Docker Compose (RECOMMENDED - includes MongoDB & RabbitMQ)
cd src/Services/UserService
docker-compose up -d --build

# Run the PetService API (listens on ports configured in launchSettings.json)
dotnet run --project src/Services/PetService/PetAdoption.PetService.API

# Run the Blazor Web App
dotnet run --project src/Web/PetAdoption.Web.BlazorApp

# Start all infrastructure (MongoDB, RabbitMQ) and the service via Docker Compose
docker compose up

# Run only infrastructure dependencies
docker compose up mongo rabbitmq

# Run tests
dotnet test PetAdoption.sln

# View UserService logs
docker logs petadoption-userservice -f

# Access RabbitMQ Management UI
# http://localhost:15672 (guest/guest)
```

## Architecture

This is a **.NET 10** microservice platform for pet adoption, implementing **Clean Architecture**, **tactical DDD patterns**, **CQRS**, **Transactional Outbox**, **JWT Authentication**, **Role-Based Authorization**, and a **custom Mediator pattern** (not MediatR).

## Project Status

| Service | Status | E2E Verified | Documentation |
|---------|--------|--------------|---------------|
| **UserService** | ✅ **Complete** | ✅ 100% Passed | `src/Services/UserService/E2E-VERIFICATION-COMPLETE.md` |
| **PetService** | 🚧 In Progress | ⏸️ Not Started | - |
| **AdoptionService** | 📋 Planned | ⏸️ Not Started | - |

### Solution Structure

```
PetAdoption/
├── src/
│   ├── Services/
│   │   ├── UserService/                              ✅ COMPLETE
│   │   │   ├── PetAdoption.UserService.Domain/       (Class Library - Zero dependencies)
│   │   │   ├── PetAdoption.UserService.Application/  (Class Library - References Domain)
│   │   │   ├── PetAdoption.UserService.Infrastructure/ (Class Library - References Application + Domain)
│   │   │   ├── PetAdoption.UserService.API/          (Web API - References all layers)
│   │   │   ├── docker-compose.yml                    (Docker setup with MongoDB & RabbitMQ)
│   │   │   ├── E2E-VERIFICATION-COMPLETE.md          (100% test results)
│   │   │   └── README.md                             (Service documentation)
│   │   └── PetService/                               🚧 IN PROGRESS
│   │       ├── PetAdoption.PetService.Domain/        (Class Library - Zero dependencies)
│   │       ├── PetAdoption.PetService.Application/   (Class Library - References Domain)
│   │       ├── PetAdoption.PetService.Infrastructure/ (Class Library - References Application + Domain)
│   │       └── PetAdoption.PetService.API/           (Web API - References all layers)
│   ├── Web/
│   │   └── PetAdoption.Web.BlazorApp/                (Blazor Web App)
│   └── ServiceCommon/                                 (Future: Shared libraries)
├── tests/
│   ├── UserService/
│   │   └── PetAdoption.UserService.UnitTests/
│   └── PetService/
│       ├── PetAdoption.PetService.UnitTests/
│       └── PetAdoption.PetService.IntegrationTests/
├── docs/
│   ├── CLAUDE.md                                      (This file)
│   ├── ERROR_HANDLING.md
│   ├── architecture/                                  (Architecture documentation)
│   └── message-broker/                                (RabbitMQ topology docs)
└── docker-compose.yml
```

### Dependency Rules (Clean Architecture)

```
API → Infrastructure → Application → Domain
Infrastructure → Domain
Application → Domain
Domain → (no external dependencies)
```

## UserService (Authentication & User Management) ✅ COMPLETE

**Status**: Production Ready | **E2E Tests**: 53/53 Passed (100%)

UserService handles user registration, authentication, authorization, and profile management with JWT-based security.

### Key Features
- ✅ User registration with email validation
- ✅ JWT authentication (HMAC SHA-256)
- ✅ Role-based authorization (User, Admin)
- ✅ BCrypt password hashing (work factor 12)
- ✅ Profile management (update profile, change password)
- ✅ Admin operations (list users, suspend, promote to admin)
- ✅ Domain events with Transactional Outbox Pattern
- ✅ MongoDB value object serialization
- ✅ RabbitMQ event publishing

### API Endpoints

#### Public Endpoints (No Authentication)
- `POST /api/users/register` — Register new user
- `POST /api/users/login` — Login and get JWT token

#### Authenticated Endpoints (Require JWT Token)
- `GET /api/users/me` — Get current user profile
- `PUT /api/users/me` — Update current user profile
- `POST /api/users/me/change-password` — Change password

#### Admin Endpoints (Require Admin Role)
- `GET /api/users` — List all users (paginated)
- `GET /api/users/{id}` — Get user by ID
- `POST /api/users/{id}/suspend` — Suspend user
- `POST /api/users/{id}/promote-to-admin` — Promote user to admin

### Domain Events Published
- `UserRegisteredEvent` → routing key: `user.registered.v1`
- `UserProfileUpdatedEvent` → routing key: `user.profile-updated.v1`
- `UserPasswordChangedEvent` → routing key: `user.password-changed.v1`
- `UserSuspendedEvent` → routing key: `user.suspended.v1`
- `UserRoleChangedEvent` → routing key: `user.role-changed.v1`

### Security Implementation
- **JWT Token**: HMAC SHA-256 with configurable secret
- **Token Claims**: userId, email, role, jti (token ID)
- **Token Expiration**: 1 hour (configurable)
- **Password Hashing**: BCrypt with work factor 12
- **Authorization Policies**:
  - `Authenticated` — requires valid JWT token
  - `AdminOnly` — requires Admin role in token

### MongoDB Value Objects Pattern ⚠️ CRITICAL

**Issue**: MongoDB's LINQ provider is incompatible with custom value object serializers.

**Solution**: Use MongoDB Filter API instead of LINQ expressions for all queries involving value objects.

**DO NOT DO THIS** (will fail at runtime):
```csharp
// ❌ WRONG - LINQ with value objects
await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
```

**DO THIS INSTEAD** (correct approach):
```csharp
// ✅ CORRECT - Filter API with value objects
var filter = Builders<User>.Filter.Eq("Email", email.Value);
await _users.Find(filter).FirstOrDefaultAsync();

var filter = Builders<User>.Filter.Eq("_id", userId.Value);
await _users.Find(filter).FirstOrDefaultAsync();
```

**Why**: Custom serializers (for Email, UserId, etc.) don't expose field structure to MongoDB's LINQ translator.

**Apply this pattern to**:
- All repository query methods
- All query store methods
- Any MongoDB query involving value objects (Email, UserId, FullName, Password, PhoneNumber)

### Testing
See `src/Services/UserService/E2E-VERIFICATION-COMPLETE.md` for complete test results.

---

## PetService (Pet Management) 🚧 IN PROGRESS

### Layer Structure

#### Domain Layer (Pure - No Dependencies)
**Project:** `PetAdoption.PetService.Domain`
- **Aggregates:** `Pet` aggregate root with full lifecycle (`Reserve()`, `Adopt()`, `CancelReservation()`)
- **Value Objects:** `PetName`, `PetType` with validation and business rules
- **Domain Events:** `PetReservedEvent`, `PetAdoptedEvent`, `PetReservationCancelledEvent` (inherit from `DomainEventBase`)
- **Domain Exceptions:** `DomainException` with error codes (see ERROR_HANDLING.md)
- **Interfaces:** `IPetRepository`, `IOutboxRepository`, `IDomainEvent`, `IAggregateRoot`, `IEntity`
- **Outbox:** `OutboxEvent` entity for reliable event delivery

#### Application Layer (Orchestration)
**Project:** `PetAdoption.PetService.Application`
- **Commands/** — `ReservePetCommand`, `AdoptPetCommand`, `CancelReservationCommand` with handlers
- **Queries/** — `GetAllPetsQuery`, `GetPetByIdQuery` with handlers
- **DTOs/** — `PetListItemDto`, `PetDetailsDto`, response DTOs, `ErrorResponse`
- **Abstractions/** — `IMediator`, `IRequest`, `IRequestHandler`, `IPipelineBehavior` (mediator contracts)

#### Infrastructure Layer (Implementation)
**Project:** `PetAdoption.PetService.Infrastructure`
- **Persistence/** — `PetRepository` (commands), `PetQueryStore` (queries), `OutboxRepository`
- **Messaging/** — `RabbitMqPublisher` (publishes to RabbitMQ)
- **BackgroundServices/** — `OutboxProcessorService` for reliable event delivery
- **Middleware/** — `ExceptionHandlingMiddleware` for error handling with subcodes
- **Mediator/** — Concrete mediator implementation, `LoggingBehavior`
- **DependencyInjection/** — `ServiceCollectionExtensions` for DI configuration

#### API Layer
**Project:** `PetAdoption.PetService.API`
- **Controllers/** — ASP.NET Core REST controllers dispatching through mediator
- All exceptions handled by `ExceptionHandlingMiddleware`
- Swagger/OpenAPI documentation

### Key Patterns

#### 1. DDD Tactical Patterns
- **Aggregate Root:** `Pet` with encapsulation (private setters, factory method `Pet.Create()`)
- **Value Objects:** `PetName` (max 100 chars, validation), `PetType` (constrained to Dog, Cat, Rabbit, Bird, Fish, Hamster)
- **Domain Events:** Raised inside aggregate methods, stored in outbox, published asynchronously
- **Domain Exceptions:** Single `DomainException` class with error codes for programmatic handling

#### 2. CQRS Separation
- **Commands:** Use `IPetRepository` (write model)
- **Queries:** Use `IPetQueryStore` (read model)
- Commands return simple DTOs, queries return detailed DTOs

#### 3. Transactional Outbox Pattern
- Domain events saved to `OutboxEvents` collection in same transaction as aggregate
- `OutboxProcessorService` background service processes pending events every 5 seconds
- Guarantees eventual consistency and reliable event delivery
- Retry logic with max 5 retries per event

**Flow:**
```
1. Command handler calls Pet.Reserve()
2. Pet raises PetReservedEvent
3. Handler saves Pet + events to outbox (transactional)
4. Background service reads outbox, publishes to RabbitMQ
5. Marks events as processed
```

#### 4. Error Handling with Subcodes
- Single `DomainException` with `PetDomainErrorCode` enum
- `ExceptionHandlingMiddleware` transforms to standardized `ErrorResponse` JSON
- Error codes mapped to HTTP status codes (404, 400, 409, 500)
- See ERROR_HANDLING.md for complete documentation

#### 5. Custom Mediator
- **Abstractions** defined in `Application/Abstractions/IMediator.cs` (interfaces only)
- **Implementation** in `Infrastructure/Mediator/Mediator.cs`
- Uses `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns
- Handlers auto-discovered via reflection in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.AddMediator()`
- Pipeline: Request → LoggingBehavior → (ValidationBehavior - disabled) → Handler

### Infrastructure Dependencies

#### MongoDB 8.0 (Primary Data Store)

**UserService Database:**
- **Database:** `UserDb`
- **Collections:**
  - `Users` — User aggregates with value objects (Email, FullName, Password, PhoneNumber)
  - `OutboxEvents` — Unpublished domain events
- **Connection:** Configured in docker-compose.yml environment variables
- **Authentication:** Required (root/example in development)

**PetService Database:**
- **Database:** `PetAdoptionDb`
- **Collections:**
  - `Pets` — Pet aggregates with value objects
  - `OutboxEvents` — Unpublished domain events
- **Connection:** Configured in `appsettings.Development.json` under `ConnectionStrings:MongoDb`

**Value Object Serialization**:
- Custom serializers defined in `Infrastructure/Persistence/Serializers/`
- Use Filter API for queries (not LINQ) - see UserService pattern above

#### RabbitMQ 4.0 (Event Bus)

**UserService Exchange:**
- **Exchange:** `user.events` (topic)
- **Routing Keys:**
  - `user.registered.v1`
  - `user.profile-updated.v1`
  - `user.password-changed.v1`
  - `user.suspended.v1`
  - `user.role-changed.v1`

**PetService Exchange:**
- **Exchange:** `pet_reservations` (fanout)
- **Events Published:**
  - `PetReservedEvent`
  - `PetAdoptedEvent`
  - `PetReservationCancelledEvent`

**Connection:**
- Host: `rabbitmq` (in Docker network) or `localhost`
- Port: 5672 (AMQP), 15672 (Management UI)
- Credentials: guest/guest (development)

### API Endpoints

All endpoints return standardized error responses with error codes on failure.

#### Queries (GET)
- `GET /api/pets` — List all pets (returns `PetListItemDto[]`)
- `GET /api/pets/{id}` — Get single pet details (returns `PetDetailsDto`)

#### Commands (POST)
- `POST /api/pets/{id}/reserve` — Reserve an available pet (returns `ReservePetResponse`)
- `POST /api/pets/{id}/adopt` — Adopt a reserved pet (returns `AdoptPetResponse`)
- `POST /api/pets/{id}/cancel-reservation` — Cancel reservation, make available again (returns `CancelReservationResponse`)

#### Response Format

**Success (200 OK):**
```json
{
  "success": true,
  "petId": "abc123...",
  "status": "Reserved"
}
```

**Error (4xx/5xx):**
```json
{
  "errorCode": "PetNotAvailable",
  "errorCodeValue": 1001,
  "message": "Pet abc123 cannot be reserved because it is Reserved.",
  "details": {
    "petId": "abc123",
    "currentStatus": "Reserved",
    "requiredStatus": "Available"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

### Domain Model

#### Pet Aggregate States
```
Available → (Reserve) → Reserved → (Adopt) → Adopted
              ↑           ↓
              └──(Cancel)─┘
```

#### Business Rules
1. **Reserve:** Pet must be Available
2. **Adopt:** Pet must be Reserved
3. **CancelReservation:** Pet must be Reserved
4. **PetName:** 1-100 characters, trimmed, non-empty
5. **PetType:** Must be Dog, Cat, Rabbit, Bird, Fish, or Hamster (case-insensitive)

### Error Codes

| Code | Value | HTTP Status | Description |
|------|-------|-------------|-------------|
| `PetNotFound` | 1003 | 404 | Pet does not exist |
| `PetNotAvailable` | 1001 | 409 | Pet is not available for reservation |
| `PetNotReserved` | 1002 | 409 | Pet is not reserved (cannot adopt/cancel) |
| `InvalidPetName` | 2001 | 400 | Pet name validation failed |
| `InvalidPetType` | 2002 | 400 | Pet type not in allowed list |

See `ERROR_HANDLING.md` for comprehensive error handling documentation.

### Background Services

#### OutboxProcessorService
- **Purpose:** Processes pending outbox events and publishes to RabbitMQ
- **Interval:** Every 5 seconds
- **Batch Size:** 100 events per batch
- **Retry Logic:** Max 5 retries per event
- **Logging:** Logs processing progress and failures

### Testing

Test projects are structured following Clean Architecture principles:

#### Unit Tests
**Project:** `tests/PetService/PetAdoption.PetService.UnitTests`
- References: `Domain`, `Application`
- Purpose: Test business logic in isolation
- Recommended coverage:
  - Domain: Aggregate behavior, value object validation, domain exceptions
  - Application: Command/query handlers with mocked repositories

#### Integration Tests
**Project:** `tests/PetService/PetAdoption.PetService.IntegrationTests`
- References: All PetService layers
- Purpose: Test full stack with real infrastructure
- Recommended coverage:
  - API endpoints with real MongoDB and RabbitMQ (use Testcontainers)
  - Outbox processor background service
  - End-to-end workflows

```bash
# Run all tests
dotnet test PetAdoption.sln

# Run unit tests only
dotnet test tests/PetService/PetAdoption.PetService.UnitTests

# Run integration tests only
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests
```

### MongoDB Collections Schema

#### Pets Collection
```json
{
  "_id": "GUID",
  "Name": "Bella",      // Serialized from PetName value object
  "Type": "Dog",        // Serialized from PetType value object
  "Status": 0           // Enum: Available=0, Reserved=1, Adopted=2
}
```

#### OutboxEvents Collection
```json
{
  "_id": "GUID",
  "EventType": "PetReservedEvent",
  "EventData": "{\"petId\":\"...\",\"petName\":\"Bella\",...}",
  "OccurredOn": "2026-02-12T10:00:00Z",
  "ProcessedOn": "2026-02-12T10:00:05Z",
  "IsProcessed": true,
  "RetryCount": 0,
  "LastError": null
}
```

### Development Workflow

1. **Make domain changes** in `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`
2. **Create command/query** in `src/Services/PetService/PetAdoption.PetService.Application/Commands` or `.../Queries`
3. **Implement handler** in the same file as command/query
4. **Add controller action** in `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`
5. **Events are automatically** saved to outbox and published
6. **Errors are automatically** transformed to ErrorResponse

### Command/Query Handler Pattern

**File Structure:**
- Each command/query has **two files**: `*Command.cs` (or `*Query.cs`) and `*CommandHandler.cs` (or `*QueryHandler.cs`)
- The **command/query file** contains ONLY the request record
- The **handler file** contains the response record AND the handler implementation

**Example:**
```csharp
// CreatePetCommand.cs - Contains only the request
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetCommand(string Name, Guid PetTypeId) : ICommand<CreatePetResponse>;
```

```csharp
// CreatePetCommandHandler.cs - Contains response record AND handler
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetResponse(Guid Id);

public class CreatePetCommandHandler : ICommandHandler<CreatePetCommand, CreatePetResponse>
{
    private readonly IPetRepository _petRepository;

    public CreatePetCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<CreatePetResponse> Handle(CreatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = Pet.Create(request.Name, request.PetTypeId);
        await _petRepository.Add(pet);
        return new CreatePetResponse(pet.Id);
    }
}
```

**Rationale:**
- Response records are implementation details of the handler
- Keeps command/query files minimal and focused on the request contract
- Handler file becomes self-contained with both response and implementation

### Important Notes

- **Clean Architecture:** Domain has ZERO external dependencies (no NuGet packages, no project references)
- **Dependency Flow:** API → Infrastructure → Application → Domain (never reversed)
- **Mediator Abstractions:** Defined in Application layer, implemented in Infrastructure layer
- **Persistence Ignorance:** Domain layer knows nothing about MongoDB (serializers in Infrastructure)
- **Event Reliability:** Outbox pattern ensures events are never lost, even if RabbitMQ is down
- **Error Handling:** All domain exceptions automatically transformed to HTTP responses by middleware
- **Value Objects:** Validation happens automatically in constructors
- **CQRS:** Never use repository for queries, always use query store
- **Validation Pipeline:** Currently disabled (commented out in ServiceCollectionExtensions.cs)
- **Factory Method:** Always use `Pet.Create(name, type)` to create new pets
- **Client-Side Libraries:** Managed by LibMan (`libman.json`), not committed to git

### Related Documentation

- `ERROR_HANDLING.md` — Comprehensive error handling with subcodes documentation
- `README.md` — Project overview and setup instructions
- `src/Services/UserService/E2E-VERIFICATION-COMPLETE.md` — UserService E2E test results
- `src/Services/UserService/README.md` — UserService setup and API documentation
- `docs/architecture/` — Architecture decision records and diagrams
- `docs/message-broker/` — RabbitMQ topology documentation

## Critical Patterns & Lessons Learned

### 1. MongoDB Value Object Serialization (UserService)

**Problem**: MongoDB's LINQ provider cannot translate queries with custom value object serializers.

**Error You'll See**:
```
System.NotSupportedException: Serializer for Email does not represent members as fields
```

**Root Cause**: Custom serializers hide internal field structure, LINQ translator needs field access.

**Solution**: Use MongoDB Filter API for ALL queries involving value objects.

```csharp
// ❌ WRONG - Will fail at runtime
var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

// ✅ CORRECT - Always works
var filter = Builders<User>.Filter.Eq("Email", email.Value);
var user = await _users.Find(filter).FirstOrDefaultAsync();
```

**When to Use Filter API**:
- Any query with value objects (Email, UserId, FullName, etc.)
- Equality checks: `Filter.Eq("field", value)`
- Empty filters: `Filter.Empty` (for "get all" queries)
- Sorting: `Sort.Ascending("field")` or `Sort.Descending("field")`
- Compound queries: `Filter.And(filter1, filter2)`

**Already Implemented Correctly In**:
- ✅ `UserService/Infrastructure/Persistence/UserRepository.cs`
- ✅ `UserService/Infrastructure/Persistence/UserQueryStore.cs`
- ✅ `UserService/Infrastructure/Persistence/OutboxRepository.cs`

### 2. Transactional Outbox Pattern (Both Services)

**Purpose**: Guarantee domain events are published, even if message broker is down.

**Flow**:
1. Domain aggregate raises event (stored in aggregate's `DomainEvents` list)
2. Repository saves aggregate + events to outbox in same transaction
3. Background service (`OutboxProcessorService`) polls outbox every 5 seconds
4. Publishes events to RabbitMQ, marks as processed
5. Retry logic handles temporary failures (max 5 retries)

**Benefits**:
- No lost events (guaranteed delivery)
- Survives RabbitMQ downtime
- Eventual consistency guaranteed
- Idempotent event processing

### 3. JWT Authentication & Authorization (UserService)

**Token Generation**:
- Algorithm: HMAC SHA-256
- Claims: `sub` (userId), `email`, `role`, `jti` (token ID)
- Expiration: 1 hour (configurable)
- Issuer/Audience validation enabled

**Authorization Policies**:
```csharp
// Require any authenticated user
[Authorize]

// Require admin role
[Authorize(Policy = "AdminOnly")]

// Allow anonymous
[AllowAnonymous]
```

**Policy Setup** (in `Program.cs`):
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

### 4. BCrypt Password Hashing (UserService)

**Configuration**:
- Work factor: 12 (recommended for 2026)
- Hash format: `$2a$12${salt}{hash}` (60 characters)
- Never store plaintext passwords

**Implementation**:
```csharp
// Hash password
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);

// Verify password
var isValid = BCrypt.Net.BCrypt.Verify(plaintext, hashedPassword);
```

### 5. Clean Architecture Dependency Rules

**CRITICAL**: Always follow dependency flow:
```
API → Infrastructure → Application → Domain
                    ↘               ↗
                      Infrastructure → Domain
```

**Rules**:
- ✅ Domain has ZERO external dependencies (no NuGet packages)
- ✅ Application references only Domain
- ✅ Infrastructure references Application + Domain
- ✅ API references all layers (composition root)
- ❌ NEVER let Domain reference Application or Infrastructure
- ❌ NEVER let Application reference Infrastructure

### 6. CQRS Separation

**Commands** (write operations):
- Use `IRepository` (write model)
- Return simple success/failure responses
- Raise domain events
- Example: `RegisterUserCommand`, `ChangePasswordCommand`

**Queries** (read operations):
- Use `IQueryStore` (read model)
- Return detailed DTOs
- No domain events
- Example: `GetUserByIdQuery`, `GetUsersQuery`

**Benefits**:
- Optimized read/write models
- Clear separation of concerns
- Easier to scale independently

### 7. Docker Development Workflow

**UserService Docker Setup**:
```bash
cd src/Services/UserService
docker-compose up -d --build    # Build and start all containers
docker logs petadoption-userservice -f  # View logs
docker exec petadoption-mongodb mongosh -u root -p example --authenticationDatabase admin UserDb  # Access MongoDB
```

**Containers**:
- `petadoption-userservice` — .NET 10 API (port 5001)
- `petadoption-mongodb` — MongoDB 8.0 (port 27017)
- `petadoption-rabbitmq` — RabbitMQ 4.0 with management (ports 5672, 15672)

**Rebuild After Code Changes**:
```bash
docker-compose down
docker-compose up -d --build
```

### 8. Testing Strategy

**E2E Verification Approach** (UserService):
1. Build and start Docker containers
2. Test public endpoints (register, login)
3. Test authenticated endpoints (profile, update, change password)
4. Test admin endpoints (list, suspend, promote)
5. Verify data persistence in MongoDB
6. Verify events in outbox
7. Verify events published to RabbitMQ

**Result**: 53/53 tests passed (100% success rate)

See `src/Services/UserService/E2E-VERIFICATION-COMPLETE.md` for full results.

## Next Steps for Development

### Immediate Priorities
1. ✅ UserService — **COMPLETE** (100% tested, production ready)
2. 🚧 PetService — **IN PROGRESS** (adapt patterns from UserService)
3. 📋 AdoptionService — **PLANNED** (will integrate User + Pet services)

### PetService Adaptations Needed
When implementing PetService, apply UserService patterns:
- ✅ Use Filter API for MongoDB queries (not LINQ with value objects)
- ✅ Implement Transactional Outbox Pattern
- ✅ Add proper error handling with domain exceptions
- ✅ Create Docker Compose setup
- ✅ Write E2E verification plan and tests
- ✅ Document all endpoints and domain events

### Service Integration
Once both services are complete:
- Event-driven communication via RabbitMQ
- UserService publishes user events
- PetService publishes pet events
- AdoptionService subscribes to both
- JWT tokens shared across services (same secret)

## Quick Reference

### UserService Quick Test
```bash
# Register user
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","fullName":"Test User","password":"SecurePass123!","phoneNumber":"+1234567890"}'

# Login
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"SecurePass123!"}'

# Get profile (use token from login)
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer {token}"
```

### MongoDB Quick Access
```bash
# Access MongoDB with authentication
docker exec petadoption-mongodb mongosh -u root -p example --authenticationDatabase admin UserDb

# Count users
db.Users.countDocuments({})

# View outbox events
db.OutboxEvents.find().pretty()

# Count processed events
db.OutboxEvents.countDocuments({IsProcessed: true})
```

### RabbitMQ Management
- URL: http://localhost:15672
- Username: guest
- Password: guest
- Check exchanges, queues, and published messages
