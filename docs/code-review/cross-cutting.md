# Cross-Cutting Code Review

## Code Duplicated Between Services

**1. `RabbitMqOptions` + `ExchangeConfig` + `QueueConfig` + `BindingConfig` — byte-for-byte identical** (acceptable: separate bounded contexts; could be shared)
- `src/Services/PetService/.../Messaging/Configuration/RabbitMqOptions.cs`
- `src/Services/UserService/.../Messaging/Configuration/RabbitMqOptions.cs`
- Recommendation: Move to `ServiceDefaults` or a shared messaging package. Low risk since it's pure configuration DTOs.

**2. `RabbitMqTopologyBuilder` — byte-for-byte identical**, including the PRECONDITION_FAILED (406) catch-and-recreate logic (should be shared)
- `src/Services/PetService/.../Messaging/RabbitMqTopologyBuilder.cs`
- `src/Services/UserService/.../Messaging/RabbitMqTopologyBuilder.cs`
- Recommendation: Move to `ServiceDefaults`. Any bug fix must currently be applied twice.

**3. `RabbitMqTopologySetup` (IHostedService) — functionally identical** retry-on-startup pattern (10 retries, 2s delay) (should be shared)
- `src/Services/PetService/.../Messaging/RabbitMqTopologySetup.cs`
- `src/Services/UserService/.../Messaging/RabbitMqTopologySetup.cs`
- Recommendation: Move to `ServiceDefaults`.

**4. Aspire RabbitMQ bridge (`PostConfigure<RabbitMqOptions>`) — copy-pasted in both `Program.cs`** (should be a shared extension method)
- `src/Services/PetService/PetAdoption.PetService.API/Program.cs:28–42`
- `src/Services/UserService/PetAdoption.UserService.API/Program.cs:17–31`
- Recommendation: Extract `builder.Services.AddRabbitMqFromAspire()` to `ServiceDefaults`.

**5. JWT bearer authentication setup — mostly duplicated** in both `Program.cs` files (acceptable; extractable into `ServiceDefaults`)
- Acceptable duplication given bounded context separation.
- Recommendation: Extract `builder.Services.AddJwtBearerAuth(jwtSecret)` to `ServiceDefaults` to keep both in sync.

**6. CORS setup — near-identical** in both `Program.cs` files (acceptable — PetService has `.AllowCredentials()` for SignalR)
- The SignalR difference is a legitimate reason to keep these separate. Acceptable.

**7. `ExceptionHandlingMiddleware` — structurally identical skeleton, different domain knowledge** (acceptable but placement is inconsistent)
- PetService puts middleware in `Infrastructure` layer — a HTTP concern in the wrong layer.
- UserService correctly puts it in the `API` layer.
- Recommendation: Move PetService's middleware to its `API` layer for consistency.

**8. `OutboxEvent` entity — parallel but intentionally divergent** (acceptable)
- Different design choices: `Guid` vs `string` Id, behavioral vs. anemic, stored routing key vs. derived.
- Acceptable per bounded context boundaries.

**9. `DomainEventBase` — class vs. record, different properties** (acceptable)
- Acceptable per bounded context boundaries.

**10. DevDataSeeder org GUIDs — same literals hardcoded in both services** (should be shared)
- Silent coupling: if one service changes its seeded org GUID the other breaks.
- Recommendation: Share via a `DevSeedIds` constants file in `ServiceDefaults`.

**11. EF Core `HasConversion` pattern — cosmetically similar** (acceptable; domain-specific by design)

**12. `IOutboxRepository` method naming inconsistency** (should be fixed in PetService)
- PetService uses sync-style names without `Async` suffix (`Add`, `GetPendingEvents`, `Update`) despite returning `Task`.
- UserService is consistently `Async`-suffixed throughout.

## Aspire / ServiceDefaults Issues

- **JWT issuer/audience are magic string literals in `AppHost.cs`**, repeated 4 times — should be local constants.

- **Fixed service ports** (8080, 5001) appear without constants across AppHost, CORS config, and Blazor appsettings. A single change requires updating multiple files.

- **Stale MongoDB connection strings** in PetService `appsettings.Development.json` and `appsettings.Production.json` — leftover from a previous implementation. No MongoDB client is registered anywhere; these are dead config.

- **Health endpoints only exposed in Development** in `ServiceDefaults/Extensions.cs` — silently disables them in production/staging, breaking container orchestrator health probes.

- **Service discovery registered but unused** — `AddServiceDiscovery()` and `http.AddServiceDiscovery()` in `ServiceDefaults` are dead code for this solution's topology (Blazor uses fixed ports, services don't call each other).

- **Standard resilience handler applied globally** — the only `HttpClient` consumer is `GoogleTokenValidator`; this implicit behavior is worth documenting.

## Architectural Inconsistencies

- **Custom mediator (PetService) vs. direct `[FromServices]` handler injection (UserService)** — intentional per CLAUDE.md but increases cognitive overhead. UserService has no equivalent of `LoggingBehavior` timing or pipeline behaviors.

- **`DomainException` error response shape diverges**: PetService returns `{ errorCode, message, details, timestamp }`; UserService returns `{ errorCode, message }`. Clients consuming both APIs get different error shapes for the same concept.

- **`ExceptionHandlingMiddleware` placement**: PetService puts it in `Infrastructure` (HTTP concern in wrong layer); UserService correctly puts it in `API`.

- **Outbox processor architecture diverges**: PetService uses `IEventPublisher` abstraction + reflection-based deserialization (silently skips events on type-not-found); UserService stores routing keys and publishes raw bytes directly.

- **`IUnitOfWork` missing in UserService**: PetService's `IUnitOfWork` enables atomic multi-repository operations. UserService's repositories each call `SaveChangesAsync` independently — if the second save fails after the first succeeds, the aggregate is saved without its domain event, breaking the transactional outbox guarantee.

## Missing Shared Abstractions

- **Shared RabbitMQ infrastructure package** — covers `RabbitMqOptions`, `RabbitMqTopologyBuilder`, `RabbitMqTopologySetup`, and the Aspire bridge extension (low risk, high value).

- **Shared JWT bearer configuration extension method** in `ServiceDefaults`.

- **`DevSeedIds` constants in `ServiceDefaults`** — prevents silent cross-service GUID coupling in dev data seeders.

- **Standardized `ErrorResponse` DTO** — PetService's richer version (`errorCode`, `message`, `details`, `timestamp`) should be adopted by UserService for consistent client-side error handling.

- **`IUnitOfWork` in UserService** — critical for transactional outbox correctness. UserService should adopt the same pattern as PetService to prevent aggregate-saved-without-event scenarios.
