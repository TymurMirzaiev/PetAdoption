# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build PetAdoption.sln

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
```

## Architecture

This is a .NET 9 microservice for pet adoption, implementing **Clean Architecture**, **tactical DDD patterns**, **CQRS**, **Transactional Outbox**, and a **custom Mediator pattern** (not MediatR).

### Solution Structure

```
PetAdoption/
├── src/
│   ├── Services/
│   │   └── PetService/
│   │       ├── PetAdoption.PetService.Domain/        (Class Library - Zero dependencies)
│   │       ├── PetAdoption.PetService.Application/   (Class Library - References Domain)
│   │       ├── PetAdoption.PetService.Infrastructure/ (Class Library - References Application + Domain)
│   │       └── PetAdoption.PetService.API/           (Web API - References all layers)
│   ├── Web/
│   │   └── PetAdoption.Web.BlazorApp/                (Blazor Web App)
│   └── ServiceCommon/                                 (Future: Shared libraries)
├── tests/
│   └── PetService/
│       ├── PetAdoption.PetService.UnitTests/
│       └── PetAdoption.PetService.IntegrationTests/
├── docs/
│   ├── CLAUDE.md
│   └── ERROR_HANDLING.md
└── docker-compose.yml
```

### Dependency Rules (Clean Architecture)

```
API → Infrastructure → Application → Domain
Infrastructure → Domain
Application → Domain
Domain → (no external dependencies)
```

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

#### MongoDB 7.0 (Primary Data Store)
- **Database:** `PetAdoptionDb`
- **Collections:**
  - `Pets` — Pet aggregates with value objects
  - `OutboxEvents` — Unpublished domain events
- **Connection:** Configured in `appsettings.Development.json` under `ConnectionStrings:MongoDb`

#### RabbitMQ 3 (Event Bus)
- **Exchange:** `pet_reservations` (fanout)
- **Events Published:**
  - `PetReservedEvent`
  - `PetAdoptedEvent`
  - `PetReservationCancelledEvent`
- **Connection:** Configured in `appsettings.Development.json` under `RabbitMq:ConnectionString`

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
