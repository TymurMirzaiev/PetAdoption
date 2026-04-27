# PetService CLAUDE.md

## Overview

Pet lifecycle management service (.NET 9.0). Handles pet creation, reservation, adoption, and pet type administration.

## Build & Test

```bash
dotnet run --project PetAdoption.PetService.API
dotnet test tests/PetService/PetAdoption.PetService.UnitTests
dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests
```

## Domain Model

### Pet Aggregate (states)

```
Available → (Reserve) → Reserved → (Adopt) → Adopted
              ↑           ↓
              └──(Cancel)─┘
```

- Factory: `Pet.Create(name, petTypeId)`
- State transitions raise domain events
- Optimistic concurrency via `Version` field

### PetType Entity

- `PetType.Create(code, name)` — code auto-lowercased, unique
- Lifecycle: Active ↔ Inactive (Deactivate/Activate)
- Default seeded types: dog, cat, rabbit, bird, fish, hamster (`PetTypeSeeder`)

### Value Objects

- `PetName`: 1-100 chars, trimmed, non-empty

### Domain Events

- `PetReservedEvent`, `PetAdoptedEvent`, `PetReservationCancelledEvent`
- Published via transactional outbox → RabbitMQ (`pet.events` exchange)

## Architecture

### Custom Mediator (not MediatR)

- Abstractions in `Application/Abstractions/IMediator.cs`
- Implementation in `Infrastructure/Mediator/Mediator.cs`
- Handlers auto-discovered via reflection in `ServiceCollectionExtensions.AddMediator()`
- Pipeline: Request → LoggingBehavior → Handler

### Error Handling

`ExceptionHandlingMiddleware` maps `DomainException.ErrorCode` to HTTP status:

| Error Code | HTTP Status |
|------------|-------------|
| `pet_not_found`, `pet_type_not_found` | 404 |
| `invalid_pet_name`, `invalid_pet_type` | 400 |
| `pet_not_available`, `pet_not_reserved`, `concurrency_conflict`, `pet_type_already_exists` | 409 |

### CQRS

- Commands: `IPetRepository` (write), `IPetTypeRepository` (write)
- Queries: `IPetQueryStore` (read)

## API Endpoints

### Pets (`/api/pets`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/pets` | List all pets |
| POST | `/api/pets` | Create pet |
| GET | `/api/pets/{id}` | Get pet by ID |
| POST | `/api/pets/{id}/reserve` | Reserve pet |
| POST | `/api/pets/{id}/adopt` | Adopt pet |
| POST | `/api/pets/{id}/cancel-reservation` | Cancel reservation |

### Pet Types Admin (`/api/admin/pet-types`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/pet-types` | List types (`?includeInactive=true`) |
| POST | `/api/admin/pet-types` | Create type |
| GET | `/api/admin/pet-types/{id}` | Get type by ID |
| PUT | `/api/admin/pet-types/{id}` | Update type name |
| POST | `/api/admin/pet-types/{id}/deactivate` | Deactivate type |
| POST | `/api/admin/pet-types/{id}/activate` | Activate type |

## MongoDB

- Database: `PetAdoptionDb`
- Collections: `Pets`, `PetTypes`, `OutboxEvents`
- Custom BSON serializer for `PetName` value object
- PetTypeSeeder runs on startup (seeds 6 default types)

## Testing Notes

- Integration tests use Testcontainers MongoDB
- PetTypeSeeder seeds default types on app startup — test helpers must handle existing types gracefully (check for 409 Conflict, fall back to GET)
- No auth required on any endpoint (no JWT)
