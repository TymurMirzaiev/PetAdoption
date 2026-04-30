# PetService CLAUDE.md

## Overview

Pet lifecycle management service (.NET 9.0). Handles pet creation, reservation, adoption, pet type administration, user favorites, and announcements.

## Build & Test

```bash
# Run via Aspire (recommended)
dotnet run --project src/Aspire/PetAdoption.AppHost

# Run standalone
dotnet run --project src/Services/PetService/PetAdoption.PetService.API

# Tests
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests
```

## Aspire Integration

- References `PetAdoption.ServiceDefaults` for health checks, OpenTelemetry, resilience
- `builder.AddServiceDefaults()` in Program.cs, `app.MapDefaultEndpoints()` for `/health` and `/alive`
- RabbitMQ connection bridged via `PostConfigure<RabbitMqOptions>` (parses Aspire's AMQP URI into custom options)
- Fixed port 8080 (Blazor WASM connects directly, no service discovery)
- SQL Server connection string: `ConnectionStrings:SqlServer`

## Domain Model

### Pet Aggregate (states)

```
Available → (Reserve) → Reserved → (Adopt) → Adopted
              ↑           ↓
              └──(Cancel)─┘
```

- Factory: `Pet.Create(name, petTypeId, breed?, ageMonths?, description?)`
- Methods: `Reserve()`, `Adopt()`, `CancelReservation()`, `UpdateName()`, `UpdateBreed()`, `UpdateAge()`, `UpdateDescription()`, `EnsureCanBeDeleted()`
- State transitions raise domain events
- Optimistic concurrency via `Version` field

### PetType Entity

- `PetType.Create(code, name)` — code auto-lowercased, unique
- Lifecycle: Active ↔ Inactive (Deactivate/Activate)
- Default seeded types: dog, cat, rabbit, bird, fish, hamster (`PetTypeSeeder`)

### Favorite Entity

- `Favorite.Create(userId, petId)` — validates non-empty GUIDs
- Unique compound index on (UserId, PetId)
- No domain events

### Announcement Entity

- `Announcement.Create(title, body, startDate, endDate, createdBy)` — validates dates (end > start)
- `Update(title, body, startDate, endDate)` method
- Value objects: `AnnouncementTitle` (1-200 chars), `AnnouncementBody` (1-5000 chars)

### Value Objects

- `PetName`: 1-100 chars, trimmed, non-empty
- `PetBreed`: 1-100 chars, trimmed, non-empty (nullable on Pet)
- `PetAge`: non-negative int Months (nullable on Pet)
- `PetDescription`: 1-2000 chars, trimmed, non-empty (nullable on Pet)
- `AnnouncementTitle`: 1-200 chars, trimmed, non-empty
- `AnnouncementBody`: 1-5000 chars, trimmed, non-empty

### Domain Events

- `PetReservedEvent`, `PetAdoptedEvent`, `PetReservationCancelledEvent`
- Published via transactional outbox → RabbitMQ (`pet.events` exchange)

## Architecture

### Custom Mediator (not MediatR)

- Abstractions in `Application/Abstractions/IMediator.cs`
- Implementation in `Infrastructure/Mediator/Mediator.cs`
- Handlers auto-discovered via reflection in `ServiceCollectionExtensions.AddMediator()`
- Pipeline: Request → LoggingBehavior → Handler

### Authentication & Authorization

- JWT Bearer authentication (shared secret with UserService)
- Config: `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience` in appsettings
- Policies: `AdminOnly` (requires "Admin" role)
- CORS configured via `Cors:AllowedOrigins` in appsettings

### Error Handling

`ExceptionHandlingMiddleware` maps `DomainException.ErrorCode` to HTTP status:

| Error Code | HTTP Status |
|------------|-------------|
| `pet_not_found`, `pet_type_not_found`, `favorite_not_found`, `announcement_not_found` | 404 |
| `invalid_pet_name`, `invalid_pet_type`, `invalid_pet_breed`, `invalid_pet_age`, `invalid_pet_description`, `invalid_announcement_title`, `invalid_announcement_body`, `invalid_announcement_dates` | 400 |
| `pet_not_available`, `pet_not_reserved`, `pet_cannot_be_deleted`, `concurrency_conflict`, `invalid_operation`, `pet_type_already_exists`, `favorite_already_exists` | 409 |

### CQRS

- Commands: `IPetRepository`, `IPetTypeRepository`, `IFavoriteRepository`, `IAnnouncementRepository` (write)
- Queries: `IPetQueryStore`, `IFavoriteQueryStore`, `IAnnouncementQueryStore` (read)

## API Endpoints

### Pets (`/api/pets`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/pets` | Anonymous | List pets (filtered, paginated: `?status=&petTypeId=&skip=&take=`) |
| POST | `/api/pets` | Admin | Create pet (name, petTypeId, breed?, ageMonths?, description?) |
| GET | `/api/pets/{id}` | Anonymous | Get pet by ID |
| PUT | `/api/pets/{id}` | Admin | Update pet (name, breed?, ageMonths?, description?) |
| DELETE | `/api/pets/{id}` | Admin | Delete pet (only Available) |
| POST | `/api/pets/{id}/reserve` | Authenticated | Reserve pet |
| POST | `/api/pets/{id}/adopt` | Authenticated | Adopt pet |
| POST | `/api/pets/{id}/cancel-reservation` | Authenticated | Cancel reservation |

### Pet Types Admin (`/api/admin/pet-types`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/admin/pet-types` | Admin | List types (`?includeInactive=true`) |
| POST | `/api/admin/pet-types` | Admin | Create type |
| GET | `/api/admin/pet-types/{id}` | Admin | Get type by ID |
| PUT | `/api/admin/pet-types/{id}` | Admin | Update type name |
| POST | `/api/admin/pet-types/{id}/deactivate` | Admin | Deactivate type |
| POST | `/api/admin/pet-types/{id}/activate` | Admin | Activate type |

### Favorites (`/api/favorites`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/favorites` | Authenticated | Add favorite (petId in body) |
| DELETE | `/api/favorites/{petId}` | Authenticated | Remove favorite |
| GET | `/api/favorites` | Authenticated | List user's favorites (paginated: `?skip=&take=`) |

### Announcements (`/api/announcements`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/announcements` | Admin | List all announcements (paginated) |
| POST | `/api/announcements` | Admin | Create announcement |
| GET | `/api/announcements/{id}` | Admin | Get announcement by ID |
| PUT | `/api/announcements/{id}` | Admin | Update announcement |
| DELETE | `/api/announcements/{id}` | Admin | Delete announcement |
| GET | `/api/announcements/active` | Anonymous | List active announcements |

## Database (SQL Server + EF Core)

- Database: `PetAdoptionDb`
- DbContext: `PetServiceDbContext` in `Infrastructure/Persistence/`
- Tables: `Pets`, `PetTypes`, `OutboxEvents`, `Favorites`, `Announcements`
- Value objects mapped via `HasConversion` (PetName→string, PetBreed→string, PetAge→int, etc.)
- Concurrency: `Pet.Version` as concurrency token
- Indexes: unique Code on PetType, unique compound (UserId, PetId) on Favorite
- PetTypeSeeder runs on startup (seeds 6 default types)
- `EnsureCreatedAsync()` on startup (no migrations)

## Testing Notes

- Integration tests use Testcontainers SQL Server
- `PetServiceWebAppFactory` configures JWT settings and provides `GenerateTestToken()` helper
- PetTypeSeeder seeds default types on app startup — test helpers must handle existing types gracefully (check for 409 Conflict, fall back to GET)
- Bearer token required for authenticated/admin endpoints in tests
