# UserService Source Code Review

## Dead Code

- `src/Services/UserService/PetAdoption.UserService.Domain/Interfaces/IUserRepository.cs:10` — `ExistsWithEmailAsync(Email email)` is declared and implemented in `UserRepository` but **never called** anywhere. `RegisterUserCommandHandler` calls `GetByEmailAsync` and null-checks instead.

- `src/Services/UserService/PetAdoption.UserService.Application/Abstractions/IJwtTokenGenerator.cs:10` — Parameters `organizationId` and `orgRole` on `GenerateToken(...)` are **always passed as `null`** at every call site (`LoginCommandHandler`, `GoogleAuthCommandHandler`, `RefreshTokenCommandHandler`). They are dead parameters.

- `src/Services/UserService/PetAdoption.UserService.Infrastructure/Security/JwtOptions.cs:9` — `RefreshTokenLifetimeDays` on `JwtOptions` is never read. Handlers use `JwtApplicationOptions` for this value; the Infrastructure copy is unused.

- `src/Services/UserService/PetAdoption.UserService.Infrastructure/Persistence/OutboxRepository.cs:16` — `IOutboxRepository.AddAsync` is implemented but **never called in production code**. The actual outbox write goes through `UserRepository.SaveAsync` directly via EF Core, bypassing this method.

- `src/Services/UserService/PetAdoption.UserService.Application/Queries/GetUserByEmailQuery.cs` — `GetUserByEmailQueryHandler` is registered in DI but **no controller endpoint** dispatches a `GetUserByEmailQuery`. It is unreachable from the API.

- `src/Services/UserService/PetAdoption.UserService.Domain/Entities/User.cs:117` — `hasChanges` is set unconditionally to `true` on the PhoneNumber assignment, so the `if (hasChanges)` guard is always entered. The variable is dead as a meaningful guard.

## Duplicated Code

- **`User` → `UserDto` mapping appears twice, identically.** `GetUserByIdQueryHandler` and `GetUserByEmailQueryHandler` contain word-for-word identical 20-line projection code including `UserPreferencesDto` construction. Any new field must be added in two places.

- **Refresh token creation + JWT generation triplicated.** The two-line pattern `RefreshToken.Create(user.Id.Value, ...) + _refreshTokenRepo.SaveAsync(...)` and `_jwtGenerator.GenerateToken(...)` appear verbatim in `LoginCommandHandler`, `GoogleAuthCommandHandler`, and `RefreshTokenCommandHandler`.

- **`UserId.From` + `GetByIdAsync` + `UserNotFoundException` repeated in four handlers.** The exact same three-line user-fetch pattern appears in `SuspendUserCommandHandler`, `PromoteToAdminCommandHandler`, `UpdateUserProfileCommandHandler`, and `ChangePasswordCommandHandler`.

- **EF Core upsert detection logic duplicated in `UserRepository.SaveAsync` and `RefreshTokenRepository.SaveAsync`.** The `_db.Entry(entity).State == EntityState.Detached → AnyAsync → Update/Add` block is copy-pasted between both repositories.

- **Organization "not found" return duplicated across three handlers.** `UpdateOrganizationCommandHandler`, `ActivateOrganizationCommandHandler`, and `DeactivateOrganizationCommandHandler` all do `if (org is null) return new XxxResponse(false, "Organization not found")`.

## Bad Practices

- **Routing key mapping placed in `UserRepository`** (`UserRepository.cs:64–72`). The `GetRoutingKey(DomainEventBase)` switch maps domain event types to RabbitMQ routing key strings — a messaging concern — inside the persistence layer. This violates layer separation; it belongs in the messaging layer alongside `UserRabbitMqTopology`.

- **Organization commands return `(false, "...")` instead of throwing exceptions.** All six organization command handlers use a failure response pattern. This is inconsistent with user command handlers (which throw `UserNotFoundException`, `DuplicateEmailException`, etc.) and means callers must explicitly check `result.Success`. The organization domain is missing exception types entirely.

- **Magic string `"Google"` for `ExternalProvider`** (`User.cs:81`). Should be a constant or enum value to enable safe refactoring.

- **Magic string `"userId"` claim name in six places.** Appears in `JwtTokenGenerator.cs:36`, `UsersController.cs:90/108/133/151`, and `OrganizationsController.cs:121`. Should be a shared constant.

- **`[FromServices]` injection on all controller action parameters.** Both controllers inject every handler via `[FromServices]` on action parameters instead of constructor injection. This hides dependencies, makes unit testing awkward, and is unconventional in ASP.NET Core.

- **`[FromServices] handler = null!` on `OrganizationsController.GetAll`** (`OrganizationsController.cs:30`). The `= null!` default silences a compiler warning but provides no actual safety. If DI fails, this produces a `NullReferenceException` rather than a meaningful startup error.

- **`UnauthorizedAccessException` thrown in controllers is not caught by `ExceptionHandlingMiddleware`.** `UsersController.cs:91/109/133/151` and `OrganizationsController.cs:121` throw `UnauthorizedAccessException` when the `userId` claim is absent. The middleware only handles `DomainException` and `ArgumentException`; this produces a 500 instead of a 401/403.

- **`Bio` is a `class`, not a `record`** (`Bio.cs`). All other value objects (`Email`, `FullName`, `Password`, `PhoneNumber`, `UserId`) are `record` types with structural equality. `Bio` is a `class`, giving it reference equality — inconsistent and can cause subtle bugs in EF Core change tracking or equality comparisons.

- **N+1 query in `GetMyOrganizationsQueryHandler`** (`GetMyOrganizationsQueryHandler.cs:22–29`). Fetches all memberships then calls `GetByIdAsync` per membership in a `foreach` loop — M+1 database round-trips for a user in M organizations.

- **`JwtOptions` and `JwtApplicationOptions` are both bound from the same config section** (`ServiceCollectionExtensions.cs:47–54`). They share `ExpirationMinutes` and `RefreshTokenLifetimeDays` but are separate classes bound to `"Jwt"`. This duplicates configuration binding and creates ambiguity about the authoritative source.

- **`RegisterUserCommandHandler` uses `GetByEmailAsync` for an existence check instead of `ExistsWithEmailAsync`** (`RegisterUserCommandHandler.cs:32`). The full `User` entity is fetched and discarded; `ExistsWithEmailAsync` exists precisely for this cheaper check but is never used.

- **`UpdateProfileRequest` and `UpdateUserProfileCommand` reference `UserPreferences` (a Domain value object) directly** (`UsersController.cs:258`, `UpdateUserProfileCommand.cs:7`). This couples the API and Application contracts to the domain model, bypassing the DTO boundary layer.

## Refactoring Opportunities

- **Extract `UserMapper.ToDto(User)` static method** to eliminate the duplicated `User` → `UserDto` mapping in `GetUserByIdQueryHandler` and `GetUserByEmailQueryHandler`.

- **Extract `ITokenIssuanceService` or a helper method** for access token generation + refresh token creation + save, used by the three auth handlers.

- **Extract `GetUserOrThrowAsync(string rawId)` helper** (or add `GetByIdOrThrowAsync` to `IUserRepository`) to remove the four-handler repeated fetch pattern.

- **Extract generic EF Core upsert helper** from `UserRepository.SaveAsync` and `RefreshTokenRepository.SaveAsync` into a base class or static utility.

- **Introduce `OrganizationNotFoundException : DomainException`** so organization handlers can throw instead of returning failure responses, making them consistent with user handlers and letting the middleware handle 404s uniformly.

- **Define a `ClaimNames` constants class** with `public const string UserId = "userId"` referenced by `JwtTokenGenerator`, `UsersController`, and `OrganizationsController`.

- **Consolidate `JwtOptions` and `JwtApplicationOptions`** into one class (or have Application reference an interface), eliminating the duplicate configuration binding.

- **Add `GetByIdsAsync(IEnumerable<Guid>)` to `IOrganizationRepository`** and use it in `GetMyOrganizationsQueryHandler` to replace the N+1 loop with a single batch query.

- **Move `GetRoutingKey` from `UserRepository` to the messaging layer** (e.g., into `UserRabbitMqTopology` or an `OutboxEventFactory` in Infrastructure/Messaging).

- **Convert `OutboxEvent` to use private setters and behavior methods** (`MarkProcessed()`, `MarkFailed(string error)`) consistent with how other entities (`RefreshToken.Revoke()`) are structured.

- **Introduce a `UpdatePreferencesDto` in the Application layer** to decouple `UpdateProfileRequest` and `UpdateUserProfileCommand` from the `UserPreferences` domain value object directly.
