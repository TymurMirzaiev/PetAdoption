# PetService Source Code Review

## Dead Code

- `src/Services/PetService/.../Mediator.cs` — `LoggingRequestHandler<TRequest, TResponse>` is declared but never registered; `LoggingBehavior` is the actual pipeline behavior used.
- `src/Services/PetService/.../Queries/GetAllPetsQuery.cs` — `GetAllPetsQuery` / `GetAllPetsQueryHandler` entire file, never dispatched from any controller; only its assembly is referenced as an anchor for handler discovery.
- `src/Services/PetService/.../Services/OutboxProcessorService.cs` — `MaxRetryCount = 5` declared, never checked; events can fail forever.
- `src/Services/PetService/.../Abstractions/IEventPublisher.cs` — `IEventPublisher.PublishAsync(IEnumerable<IDomainEvent>)` overload defined and implemented, never called.
- **Always-true `bool Success` responses never inspected**: `RemoveFavoriteResponse`, `ResetSkipsResponse`, `DeletePetMediaResponse`, `ReorderPetPhotosResponse`, `SetPrimaryPhotoResponse`, `ActivatePetTypeResponse`, `DeactivatePetTypeResponse`, `UpdatePetTypeResponse` — all always return `true` and callers never check the value.
- **`BadRequest(result)` branches unreachable** in `PetsController.Reserve`, `Adopt`, `CancelReservation` — handlers always throw or succeed, never return a failure result.
- **`PetName`/`PetTag` `TooShort` branch unreachable** — after the `IsNullOrWhiteSpace` guard, a string of length 1 satisfies `MinLength = 1`, so the too-short branch can never fire.

## Duplicated Code

- **"Pet not found" load-or-throw pattern in 14 command handlers**, using two different styles (`if (pet == null) throw` vs `?? throw`). Should be `GetByIdOrThrowAsync` on `IPetRepository`.
- **`AddOutboxEvents` private method copy-pasted identically** in `PetRepository` and `AdoptionRequestRepository`.
- **`Pet → PetListItemDto` mapping block inlined** in 4 query handlers — any new field requires 4 edits.
- **Create/Update/Delete command pairs** (`Pet` vs `OrgPet`) differ only by an org ownership check; the shared logic is not extracted.
- **`GetUserId()` copy-pasted in 5 controllers** — should be on a shared base class.
- **Org claims extraction** (`reviewerOrgId`/`reviewerOrgRole`) copy-pasted 5 times inside `OrgPetsController`.
- **Metrics aggregation loop duplicated** inside `PetMetricsQueryStore`.

## Bad Practices

- **`SetOrganizationAddressCommand.ReviewerOrgId` typed as `string` instead of `Guid?`** — inconsistent with all other commands.
- **`AnnouncementsController.Create` extracts userId manually** instead of using a shared helper, with different failure semantics (silent 401 vs exception in other controllers).
- **`ChatController`, `OrganizationsController`, `OrganizationMetricsController`, `PetMediaController` define routes inline on actions with no class-level `[Route]` attribute** — inconsistent with the rest of the codebase.
- **`ReservePetCommand`, `AdoptPetCommand`, `CancelReservationCommand` use explicit constructor/property style** that defeats the positional record syntax used everywhere else.
- **10+ magic string JWT claim names** (`"userId"`, `"organizationId"`, `"orgRole"`, `"bio"`) scattered across 10+ files with no central constants class.
- **`OrgDashboardQueryStore` uses correlated `EXISTS` subqueries** for impression/swipe counts instead of a join — expensive for large datasets.
- **`PetMetricsQueryStore.GetMetricsByOrgAsync` does an O(n²) in-memory scan** instead of using the `BuildMetricsQuery` helper it already has.
- **`DeletePetMediaCommandHandler` deletes the DB record first, then deletes the file** — if the file deletion fails, the record is gone but the file is orphaned; order should be reversed (or use a compensating action).
- **`RabbitMqPublisher.GetRoutingKey` returns `string.Empty` for unregistered event types** — silently swallows those events with no error or log.
- **`OutboxProcessorService` ignores `MaxRetryCount`** — events can fail indefinitely.

## Refactoring Opportunities

- **Extract `PetServiceControllerBase`** with shared `GetUserId()`, `GetOrgId()`, `GetOrgRole()` helpers and consistent `[Route("api/...")]` pattern.
- **Add `GetByIdOrThrowAsync` to `IPetRepository`** to eliminate the 14 duplicated load-or-throw blocks.
- **Unify `CreatePet`/`CreateOrgPet`, `UpdatePet`/`UpdateOrgPet`, `DeletePet`/`DeleteOrgPet` pairs** — extract shared logic and parameterize the org ownership check.
- **Extract `MapPetToListItemDto` static helper** to the Application layer, used by all 4 query handlers.
- **Extract `OutboxEventSerializer.Enqueue`** to remove the duplicated `AddOutboxEvents` method from both repositories.
- **Replace always-true `bool Success` responses with `Unit` or void commands** — removes dead response types and makes handler intent clearer.
- **Introduce a `StringValueObject` base class** to reduce ~200 lines of boilerplate across `PetName`, `PetTag`, `PetDescription`, etc.
- **Wire up `MaxRetryCount`** in `OutboxProcessorService` to mark events as permanently failed after N attempts.
- **Replace OrgDashboard correlated subqueries** with grouped joins for impression/swipe metrics.
