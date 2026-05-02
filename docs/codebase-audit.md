# Codebase Audit

## Critical (bugs / broken behavior)

### 1. Approve adoption request does not save pet in a single transaction
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommandHandler.cs`, lines 44–47

The handler calls `pet.Reserve()` and then issues two separate `SaveChangesAsync` calls — one inside `_petRepository.Update(pet)` and one inside `_adoptionRequestRepository.UpdateAsync(adoptionRequest)`. If the second save fails, the pet is already in the `Reserved` state in the database but the adoption request is still `Pending`, leaving the system in an inconsistent state. Both aggregates share the same `PetServiceDbContext` (scoped), so a single unit-of-work save would fix this; alternatively, inject the `DbContext` directly and wrap both writes in one `SaveChangesAsync`.

### 2. ApproveAdoptionRequest reserves the pet unconditionally — double-approval is possible
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/ApproveAdoptionRequestCommandHandler.cs`, lines 38–46

`adoptionRequest.Approve()` transitions the request to `Approved` but `pet.Reserve()` throws only if the pet is not `Available`. If two org members race to approve two different requests for the same pet, the second handler will succeed in saving the adoption request as `Approved` but fail on `pet.Reserve()` (or silently succeed if the first reserve hasn't persisted yet). There is no check that the pet is still `Available` *before* transitioning the request. Add an explicit `if (pet.Status != PetStatus.Available) throw …` before calling `adoptionRequest.Approve()`, so the request state never diverges from the pet state.

### 3. Hardcoded `http://localhost:5001` in Blazor token-refresh handler
**File:** `src/Web/PetAdoption.Web.BlazorApp/Auth/AuthorizationMessageHandler.cs`, line 53

The refresh endpoint URL is hardcoded as `new HttpClient()` posting to `http://localhost:5001/api/users/refresh`. This bypasses the configured `UserApi` HTTP client (which reads from `appsettings.json`), breaks in any environment other than the developer's machine, and creates an un-disposed `HttpClient` per refresh (resource leak). Use the registered `IHttpClientFactory`/`UserApi` client and read the URL from configuration.

### 4. `InvalidOperation` domain error maps to 409 Conflict, not 422 Unprocessable
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`, line 104

`PetDomainErrorCode.InvalidOperation` (used for "pet not assigned to an organization", "only the requesting user can cancel", etc.) maps to `HttpStatusCode.Conflict`. Conflict (409) should indicate that a resource state conflict prevents the request from completing (e.g., duplicate key). For business rule violations that are not conflicts, 422 Unprocessable Entity is semantically correct. Clients currently cannot distinguish a real concurrency conflict from an authorization-style guard.

### 5. `Enum.Parse` on unvalidated `status` query param throws 500
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/AdoptionRequestsController.cs`, line 67

`Enum.Parse<AdoptionRequestStatus>(status, ignoreCase: true)` is called without a prior `Enum.TryParse`. Any invalid string (e.g., `?status=foo`) throws `ArgumentException`, which the middleware maps to a generic 500 (since `ArgumentException` is not in the error-code map and falls through to `HandleUnexpectedExceptionAsync`). Replace with `Enum.TryParse` and return `BadRequest` for an unrecognised value.

### 6. UserService `OutboxRepository.GetUnprocessedAsync` fetches failed events forever
**File:** `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OutboxRepository.cs`, lines 22–29

The query has no `RetryCount < N` filter. Events that have failed 5+ times are re-fetched every 5 seconds in `OutboxProcessorService`, which already skips them with a `continue`, but the wasted DB round-trip and the perpetual dead-letter growth is a bug in intent. PetService does filter on `RetryCount < 5` in its equivalent method. Add `.Where(e => e.RetryCount < 5)` to mirror PetService and prevent unbounded dead-letter accumulation.

---

## Missing guards / invariant violations

### 7. `take` query parameter has no upper-bound validation
**Files:** All controllers using `[FromQuery] int take` (e.g., `PetsController`, `FavoritesController`, `AdoptionRequestsController`, etc.)

A caller can pass `take=100000`, causing EF Core to load an unlimited number of rows. No cap is enforced anywhere in controllers or handlers. Add a guard such as `if (take > 100) take = 100;` or a model validation attribute to prevent runaway queries.

### 8. `TrackBatchImpressionsCommand` has no upper-bound on `PetIds` count
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/InteractionsController.cs`, lines 38–43, and `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackBatchImpressionsCommandHandler.cs`

The only validation is that `PetIds` is non-null and non-empty. A caller can post thousands of pet IDs, resulting in unbounded `INSERT` statements in a single `SaveChangesAsync`. Add a reasonable cap (e.g., 100 IDs) and return `BadRequest` for oversized payloads.

### 9. `RejectAdoptionRequestBody.Reason` is not validated in the controller
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/AdoptionRequestsController.cs`, line 90

The `Reason` field in `RejectAdoptionRequestBody` is typed `string` (non-nullable) but there is no `[Required]` attribute or null check before calling `adoptionRequest.Reject(request.Reason)`. With ASP.NET Core model binding, a missing JSON key will deserialise to an empty string or `null` depending on nullability settings, silently forwarding the guard to the domain layer rather than giving a `400` immediately at the controller.

### 10. `UserProfile.UpdateProfile` silently ignores empty `phoneNumber` string
**File:** `src/Services/UserService/PetAdoption.UserService.Domain/Entities/User.cs`, lines 104–115

`if (phoneNumber != null)` — a caller passing `""` (empty string) will invoke `PhoneNumber.FromOptional("")`, which may either validate and throw or accept the empty string depending on that value object's implementation. The guard should also clear the phone number (`null` should be used to unset it), but the current contract only updates when the field is non-null, making it impossible to remove a phone number via the update profile endpoint.

### 11. `CreateOrgPetCommandHandler` does not validate `PetTypeId` exists
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreateOrgPetCommandHandler.cs`

`CreatePetCommandHandler` also has the same omission. A pet can be created with an arbitrary `PetTypeId` GUID that has no matching `PetType` row. Downstream, the pet type name resolves to "Unknown" in list DTOs. A guard loading `_petTypeRepository.GetByIdAsync(request.PetTypeId)` and throwing `PetTypeNotFound` if absent would make the invariant explicit.

### 12. `DevDataSeeder` (UserService) creates a member with `OrgRole.Moderator` labelled as "regular member"
**File:** `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/DevDataSeeder.cs`, lines 156–159

The comment says "regular member" but `OrganizationMember.Create(orgId, ..., OrgRole.Moderator)` grants full moderator power including adoption request approval. This is a seeding inconsistency that could mask missing authorization coverage in manual testing.

---

## Hardcoded values to extract

### 13. Magic numbers: outbox batch size and processing interval scattered across two services
**Files:**
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/BackgroundServices/OutboxProcessorService.cs`, line 16 (`TimeSpan.FromSeconds(5)`) and line 53 (`batchSize: 100`)
- `src/Services/UserService/PetAdoption.UserService.Infrastructure/BackgroundServices/OutboxProcessorService.cs`, lines 52, 100, 118 (`TimeSpan.FromSeconds(5)`, `batchSize: 100`, `RetryCount >= 5`)

Processing interval, batch size, and max retry count are duplicated as inline literals across both services. Extract to named constants or to `appsettings.json` via an `OutboxOptions` class so they can be tuned in production without recompilation.

### 14. Magic number: refresh token lifetime hardcoded as 30 days in three places
**Files:**
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/LoginCommandHandler.cs`, line 72
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/GoogleAuthCommandHandler.cs`, line 55
- `src/Services/UserService/PetAdoption.UserService.Application/Commands/RefreshTokenCommandHandler.cs`, line 42

`TimeSpan.FromDays(30)` appears three times. This should be a single named constant or a configuration value in `JwtOptions` (e.g., `RefreshTokenLifetimeDays`).

### 15. Magic number: JWT access-token expiry hardcoded in `LoginResponse` as `3600`
**File:** `src/Services/UserService/PetAdoption.UserService.Application/Commands/LoginCommandHandler.cs`, line 88

`ExpiresIn: 3600` is a literal that does not read from `JwtOptions.ExpirationMinutes`. If someone changes the JWT expiration via configuration, the `ExpiresIn` field in the response will be wrong. Use `(int)TimeSpan.FromMinutes(_jwtOptions.ExpirationMinutes).TotalSeconds`.

### 16. Magic numbers: RabbitMQ topology retry constants in `RabbitMqTopologySetup`
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Messaging/RabbitMqTopologySetup.cs`, lines 25–26

`const int maxRetries = 10` and `const int delayMs = 2000` are local constants; the equivalent setup in UserService's `RabbitMqTopologySetup` likely duplicates these. Both should live in a shared options class or be identical constants defined in `RabbitMqOptions`.

### 17. Hardcoded exchange name `"user.events"` in UserService outbox processor
**File:** `src/Services/UserService/PetAdoption.UserService.Infrastructure/BackgroundServices/OutboxProcessorService.cs`, line 112

PetService uses `RabbitMqTopology.Exchanges.PetEvents` (a named constant). UserService hardcodes the string `"user.events"`. Introduce a `UserRabbitMqTopology` constants class, mirroring PetService's pattern.

---

## Performance & queries

### 18. N+1 query in `GetOrgAdoptionRequestsQueryHandler`
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetOrgAdoptionRequestsQuery.cs`, lines 47–58

For each adoption request item, the handler issues a separate `await _petQueryStore.GetById(item.PetId)`. With the default `take=20`, this produces 21 round-trips. Fix by collecting all `PetId` values first and loading them in a single query (e.g., `GetByIdsAsync`), then building a dictionary for O(1) lookup.

### 19. `GetFavoritesQueryStore` uses a correlated subquery for `PetType` lookup per row
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs`, lines 44–51

Inside the final `.Select(...)` projection, `_db.PetTypes.Where(pt => pt.Id == x.p.PetTypeId).Select(pt => pt.Name).First()` is a correlated subquery executed for every row in the page. This translates to `take` additional SQL round-trips (or a subselect per row depending on EF version). Replace by joining `PetTypes` in the main query composition.

### 20. `PetMetricsQueryStore.GetMetricsByOrgAsync` loads all pet IDs into memory first, then uses `Contains`
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetMetricsQueryStore.cs`, lines 18–22

`petIds` is loaded as a `List<Guid>` from SQL, then used in `WHERE petId IN (...)` via `Contains`. For large organizations this produces a huge `IN` clause. Consider joining on `OrganizationId` directly in the interactions query instead of pre-fetching IDs.

### 21. `ApplyTagFilterAsync` makes a raw SQL round-trip before the LINQ filter
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`, lines 142–162

The tag filter fetches all matching pet IDs via raw SQL, then hands a potentially large `List<Guid>` to LINQ `Contains`. A large tag match set produces a large `IN` clause. The logic is correct but the comment acknowledges the workaround. At minimum, the `Pets` table tag filter should add an `OrganizationId` pre-filter when called from `GetFilteredByOrg` to narrow the raw SQL result, avoiding a full-table scan on the `Tags` JSON column.

### 22. `GetDiscoverPetsQueryHandler` issues three sequential DB queries before the main query
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetDiscoverPetsQuery.cs`, lines 40–55

`GetPetIdsByUserAsync` (skips), `GetPetIdsByUserAsync` (favorites), and `GetDiscoverable` are executed sequentially. The first two could be parallelised with `Task.WhenAll`. Additionally, if a user has a large number of skips/favorites, passing a large `HashSet<Guid>` to `WHERE NOT IN (...)` in the DB will degrade. This is an architectural limitation but the sequential awaits are an easy win.

### 23. `PetTypeSeeder` loads all pet types on every startup
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetTypeSeeder.cs`, line 26

`GetAllAsync()` returns all `PetType` rows on every startup. While cheap at small scale, the seed should use a more targeted check (e.g., `AnyAsync()`) rather than loading every row.

---

## Consistency / style drift

### 24. `UpdatePetResponse` omits `Tags` field; `UpdateOrgPetResponse` includes it
**Files:**
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdatePetCommand.cs`, line 15 — `UpdatePetResponse` has no `Tags` property.
- `src/Services/PetService/PetAdoption.PetService.Application/Commands/UpdateOrgPetCommand.cs`, line 17 — `UpdateOrgPetResponse` includes `Tags`.

The two update commands are structurally equivalent (both accept `Tags`) but their responses are inconsistent. Clients calling `PUT /api/pets/{id}` cannot see the resulting tag list without an additional GET.

### 25. `CreatePetCommandHandler` does not validate `PetTypeId` exists (inconsistency vs. `TrackSkipCommandHandler`)
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/CreatePetCommandHandler.cs`

`TrackSkipCommandHandler` fetches the pet to ensure it exists before creating a skip. `CreatePetCommandHandler` and `CreateOrgPetCommandHandler` do not verify that `PetTypeId` refers to an active pet type. This is architecturally inconsistent — some commands validate related entities, others do not.

### 26. `OrganizationsController.GetAll` has a nullable default-initialised `[FromServices]` parameter
**File:** `src/Services/UserService/PetAdoption.UserService.API/Controllers/OrganizationsController.cs`, line 31

`[FromServices] IQueryHandler<...> handler = null!` — suppressing nullable with `null!` is a workaround to satisfy the compiler while still depending on DI. This pattern deviates from all other actions in the same controller and in both services. The `null!` suppressors signal the type is not null but bypass compile-time safety. All other controllers in the project use `[FromServices]` without defaults; remove `= null!`.

### 27. `Announcement` `Create` factory raises no domain event
**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Announcement.cs` (implied by the outbox pattern used for `Pet`, `AdoptionRequest`, and `Favorite`)

The pet aggregate raises `PetReservedEvent`, `PetAdoptedEvent` etc. via the outbox. Announcements have no domain events at all — creation, update, and deletion are silent from an event perspective. This is a consistency gap if downstream consumers (e.g., notifications) need to react to announcement changes.

### 28. `PetService` `OutboxProcessorService` re-reads `RetryCount` field but never persists it via `SaveChangesAsync`
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/OutboxRepository.cs`, lines 36–40

`Update(outboxEvent)` calls `_db.SaveChangesAsync()` without explicitly setting the entity as modified. Because `OutboxEvent` properties (`RetryCount`, `LastError`) are `private set`, EF Core change tracking may not detect the mutation when the entity was loaded in a different scope/instance. PetService creates `outboxEvent.RecordFailure(...)` which mutates the object in the `OutboxProcessorService` scope, then passes it to `outboxRepository.Update`. If EF's change tracker lost track of the entity between scopes this silently discards the failure record — the retry count would never increment and failing events would retry indefinitely.

### 29. `ServiceCollectionExtensions.AddMediator` has a commented-out validation behavior block
**File:** `src/Services/PetService/PetAdoption.PetService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, lines 42–56

A large commented-out block registers validators and a `ValidationBehavior` pipeline behavior that is never used. `ValidationBehavior.cs` is also entirely commented out. Either remove dead code or complete the feature. Stale commented code misleads future contributors.

### 30. `DeletePetResponse` and `DeleteOrgPetResponse` return `Success: true` and a message body on HTTP 200
**Files:** `DeletePetCommand.cs`, `DeleteOrgPetCommand.cs`

REST convention for a successful `DELETE` is `204 No Content`. Both handlers return a JSON body `{success: true, message: "..."}` with `200 OK`. The controllers pass this through with `return Ok(result)`. Align with `FavoritesController.RemoveFavorite` and `SkipsController.ResetSkips` which correctly return `NoContent()`.

---

## Security

### 31. JWT secret not validated for minimum length
**Files:** `src/Services/PetService/PetAdoption.PetService.API/Program.cs` line 95; `src/Services/UserService/PetAdoption.UserService.API/Program.cs` line 34

The JWT secret is read from configuration and used directly without checking its length. HMAC-SHA256 requires at least 256 bits (32 bytes). A short secret (e.g., `"dev"`) results in weak signatures that are trivially brutable. Add a startup guard: `if (jwtSecret.Length < 32) throw new InvalidOperationException("JWT secret must be at least 32 characters")`.

### 32. Google ID token validation calls an external HTTP endpoint; any network failure silently returns `null` (swallowed exception)
**File:** `src/Services/UserService/PetAdoption.UserService.Infrastructure/Security/GoogleTokenValidator.cs`, lines 19–37

The `catch { return null; }` block treats every failure — including transient network errors, malformed tokens, and the token being expired — identically. A caller gets `InvalidCredentialsException` in all cases. This prevents the service from distinguishing between "Google is down" (should return 503) and "token is invalid" (should return 401). Log the exception and consider re-throwing non-validation exceptions.

### 33. `Logout` endpoint does not validate that the refresh token belongs to the authenticated user
**File:** `src/Services/UserService/PetAdoption.UserService.API/Controllers/UsersController.cs`, lines 144–157

Any authenticated user can revoke any refresh token by supplying its value. The handler fetches the token, checks nothing about ownership, and revokes it. An attacker who learns another user's refresh token can silently log them out. Add a check: `if (token.UserId != currentUserId) return Forbid();`.

### 34. `GetPetMetrics` endpoint performs authorization inside the handler but not in the controller attribute
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/OrganizationMetricsController.cs`, lines 33–43

`GET /api/pets/{petId}/metrics` is protected by `[Authorize]` (any authenticated user), but the actual org-membership check is buried in `GetPetMetricsQueryHandler.Handle`. This is inconsistent with the `GetOrgMetrics` action above it which uses `[ServiceFilter(typeof(OrgAuthorizationFilter))]`. The handler authorization guard works, but the inconsistency means the filter layer provides no early rejection.

### 35. `AnnouncementsController.Create` force-parses `userId` claim with `!` null-forgiving operator
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/AnnouncementsController.cs`, line 25

`Guid.Parse(User.FindFirstValue("userId")!)` — the `!` suppresses the null warning but if the claim is absent at runtime, `Guid.Parse(null)` throws `ArgumentNullException` which is caught by the middleware as an unexpected exception and returns a 500. Replace with the same pattern used in other controllers: `FindFirstValue("userId") ?? throw new UnauthorizedAccessException(...)`.

---

## Minor / low priority

### 36. `OutboxEvent` private constructor comment says "MongoDB deserialization" — not MongoDB
**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Events/OutboxEvent.cs`, line 21; `src/Services/UserService/PetAdoption.UserService.Domain/Entities/OutboxEvent.cs` (similar)

Comment is a stale copy-paste from an earlier MongoDB prototype. The service uses SQL Server / EF Core. Update comment to say "EF Core".

### 37. `GetFavoritesQueryHandler` computes `Page` as `Skip / Take + 1`; division by zero if `take=0`
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQueryHandler.cs`, line 21

`request.Skip / request.Take + 1` will throw `DivideByZeroException` if `take=0`. While this is an unusual input, it would surface as a 500. Add a validation guard: `if (request.Take <= 0) take = 1;` (or reject in the controller).

### 38. `Pet.Create` has two overloads where the org-less one is accessible to admin (`CreatePetCommand`) but should be identical in structure
**File:** `src/Services/PetService/PetAdoption.PetService.Domain/Pet.cs`, lines 57–75

The second `Create` overload (`Create(..., Guid organizationId)`) sets `OrganizationId` by mutating after construction. This bypasses the constructor's private-set pattern and exposes a mutable `OrganizationId` setter via `AssignToOrganization`. The factory method could set it in the constructor to keep all state final.

### 39. `TrackInteractionCommandHandler` does not verify the pet exists before inserting an interaction
**File:** `src/Services/PetService/PetAdoption.PetService.Application/Commands/TrackInteractionCommandHandler.cs`

`TrackSkipCommandHandler` validates the pet exists (`_petRepository.GetById`). `TrackInteractionCommandHandler` omits this check. An interaction can be recorded for a nonexistent pet ID, polluting metrics. Align with the skip handler pattern.

### 40. `Discover` controller uses `take` parameter without an upper bound, unlike explicit paging on other endpoints
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/DiscoverController.cs`, line 34

The default is 10 but a caller can pass `take=10000`, which in `GetDiscoverPetsQueryHandler` is passed directly as `request.Take + 1` to the query store. Cap it (e.g., `take = Math.Min(take, 50)`).

### 41. `UserService.OutboxProcessorService` holds a long-lived `IConnection` and `IChannel` without reconnection logic
**File:** `src/Services/UserService/PetAdoption.UserService.Infrastructure/BackgroundServices/OutboxProcessorService.cs`, lines 56–78

The connection is created once at startup. If RabbitMQ drops the connection (restart, network blip), the background service will throw on every `BasicPublishAsync` and log errors until the host is restarted. PetService's `RabbitMqPublisher` has a `EnsureConnection()` guard that re-creates the connection when closed. Add equivalent reconnect logic in UserService's outbox processor.

### 42. No `[ApiController]`-level route on `InteractionsController` — routes are defined inline on each action
**File:** `src/Services/PetService/PetAdoption.PetService.API/Controllers/InteractionsController.cs`, lines 25, 35

All other controllers use `[Route("api/[controller]")]` at the class level. `InteractionsController` puts `[HttpPost("api/pets/{petId:guid}/interactions")]` and `[HttpPost("api/pets/interactions/batch")]` directly on each action. This works but is inconsistent and harder to maintain.
