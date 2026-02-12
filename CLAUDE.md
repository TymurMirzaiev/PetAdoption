# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build PetAdoption.sln

# Run the PetService API (listens on ports configured in launchSettings.json)
dotnet run --project PetAdoption.PetService

# Start all infrastructure (MongoDB, RabbitMQ) and the service via Docker Compose
docker compose up

# Run only infrastructure dependencies
docker compose up mongo rabbitmq
```

No test projects exist yet. When added, use `dotnet test PetAdoption.sln`.

## Architecture

This is a .NET 9 microservice for pet adoption, using **DDD**, **CQRS**, and a **custom Mediator pattern** (not MediatR).

### Layer Structure (inside `PetAdoption.PetService/`)

- **Domain/** — Aggregate roots, entities, value objects, domain events. `Pet` is the main aggregate root with `Reserve()` behavior that raises `PetReservedEvent`.
- **Application/** — CQRS handlers split into `Commands/` and `Queries/`. Each request type has a corresponding handler implementing `IRequestHandler<TRequest, TResponse>`.
- **Infrastructure/** — Repository implementations, event publishing, DB context, DI registration.
  - `Mediator/` — Custom mediator with pipeline behaviors (`LoggingBehavior`, `ValidationBehavior`). Handlers are auto-discovered via reflection in `ServiceCollectionExtensions.AddMediator()`.
- **Controllers/** — ASP.NET Core REST controllers dispatching through the mediator.

### Key Patterns

- **Custom Mediator:** Defined in `Infrastructure/Mediator/Mediator.cs`. Uses `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns. Handler registration is reflection-based via `ServiceCollectionExtensions`.
- **Domain Events:** Raised inside aggregate methods, collected on the aggregate, then published via `IEventPublisher` (RabbitMQ) after persistence.
- **Repository:** `IPetRepository` → `PetRepository` uses MongoDB driver directly (not EF Core). The EF Core `PetDbContext` and SQL Server registration in `Program.cs` are vestigial and unused.

### Infrastructure Dependencies

- **MongoDB 7.0** — Primary data store. Database: `PetAdoptionDb`, Collection: `Pets`
- **RabbitMQ 3** — Event bus using fanout exchange (`pet_reservations`)
- Connection strings configured in `appsettings.Development.json` under `ConnectionStrings:MongoDb` and `RabbitMq:ConnectionString`

### API Endpoints

- `GET /api/pets` — List all pets
- `POST /api/pets/{id}/reserve` — Reserve a pet by ID
