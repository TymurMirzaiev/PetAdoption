# PetAdoption

A .NET microservices platform for pet adoption built with Clean Architecture, DDD, CQRS, and event-driven patterns.

## Services

| Service | Framework | Description |
|---------|-----------|-------------|
| **PetService** | .NET 9.0 | Pet lifecycle (CRUD, reservations, adoptions), pet types, favorites, announcements |
| **UserService** | .NET 10.0 | Authentication (JWT + Google SSO), RBAC, user management |
| **Blazor WASM** | .NET 10.0 | Frontend SPA with MudBlazor 8.x |

## Tech Stack

- **Database**: SQL Server + EF Core (value object conversions, transactional outbox)
- **Messaging**: RabbitMQ for async domain event publishing
- **Auth**: JWT (HMAC SHA-256) + BCrypt + Google SSO
- **Orchestration**: .NET Aspire (SQL Server, RabbitMQ, all services)
- **Testing**: xUnit, FluentAssertions, Testcontainers SQL Server

## Getting Started

```bash
# Run everything via Aspire (recommended)
dotnet run --project src/Aspire/PetAdoption.AppHost

# Or run infrastructure only via Docker Compose
cd src/Services/UserService
docker-compose up -d mssql rabbitmq

# Build & test
dotnet build PetAdoption.sln
dotnet test PetAdoption.sln
```

## Project Structure

```
PetAdoption/
├── src/
│   ├── Aspire/
│   │   ├── PetAdoption.AppHost/         — Aspire orchestrator
│   │   └── PetAdoption.ServiceDefaults/ — Shared health checks, OpenTelemetry
│   ├── Services/
│   │   ├── PetService/                  — Pet management (port 8080)
│   │   └── UserService/                 — Auth & users (port 5001)
│   └── Web/
│       └── PetAdoption.Web.BlazorApp/   — Blazor WASM frontend
└── tests/
    ├── PetService/     UnitTests + IntegrationTests
    └── UserService/    UnitTests + IntegrationTests
```

## Architecture

Each service follows Clean Architecture with strict dependency rules:

```
API → Infrastructure → Application → Domain (zero external dependencies)
```

- **CQRS**: `IRepository` (write) / `IQueryStore` (read)
- **DDD**: Aggregates with factory methods, value objects, domain events
- **Transactional Outbox**: Domain events saved atomically, published via background service to RabbitMQ
- **EF Core**: `HasConversion` for value objects, `EnsureCreatedAsync` on startup (no migrations)
