# Test Suite Code Review

## Duplicated Test Code

**1. `SeedPetTypeAsync` helper duplicated across 10 PetService integration test classes**

The same or nearly-identical helper (POST to `/api/admin/pet-types`, fall back to GET-all on conflict) appears independently in:
- `tests/PetService/.../Tests/PetsControllerTests.cs:47–68`
- `tests/PetService/.../Tests/AdoptionRequestsControllerTests.cs:67–86`
- `tests/PetService/.../Tests/FavoritesControllerTests.cs:44–65`
- `tests/PetService/.../Tests/FavoritesEnhancedTests.cs:44–62`
- `tests/PetService/.../Tests/DiscoverControllerTests.cs:44–64`
- `tests/PetService/.../Tests/PetFilterTests.cs:44–62`
- `tests/PetService/.../Tests/OrgDashboardControllerTests.cs:191–207`
- `tests/PetService/.../Tests/OrgPetsControllerTests.cs:137–153`
- `tests/PetService/.../Tests/PetMediaControllerTests.cs:273–289`
- `tests/PetService/.../Tests/PetMedicalRecordControllerTests.cs:214–230`

Four of these call it with the exact same `"dog"/"Dog"` arguments.

**2. `CreatePetAsync` helper duplicated in three test classes**

The pattern of building a `CreatePetRequestBuilder`, POSTing to `/api/pets`, asserting 201, and returning the ID appears identically in `PetsControllerTests`, `FavoritesControllerTests`, and `DiscoverControllerTests`.

**3. `SqlServerFixture` / `SqlServerCollection` completely duplicated between services**

`tests/PetService/.../Infrastructure/MongoDbFixture.cs` and `tests/UserService/.../Infrastructure/MongoDbFixture.cs` are byte-for-byte identical except for the namespace. Both define `SqlServerFixture` and `SqlServerCollection` with the same `MsSqlBuilder`, `ConnectionString`, `InitializeAsync`, and `DisposeAsync` implementations.

**4. `CreateOrgPetRequestBuilder` is a structural copy of `CreatePetRequestBuilder`**

Both builders have the same six private fields and the same six `With…` methods. The only differences are the target request type and default name string.

**5. Private response DTO records duplicated across test classes**

- `CreatePetTypeResponseDto(Guid Id, string Code, string Name)` — defined independently in at least 7 test classes.
- `PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive)` — defined independently in at least 8 test classes.
- `LoginResponseDto(bool Success, string Token, string UserId, string Email, string FullName, string Role, int ExpiresIn)` — defined in `AdminUserManagementTests`, `OrganizationManagementTests`, `PasswordManagementTests`, `UserProfileTests`, and `AuthHelper`.
- `RegisterResponse(bool Success, string UserId, …)` — duplicated between `AdminUserManagementTests` and `OrganizationManagementTests`.

**6. `SeedPetWithOrgAsync` pattern repeated in four test classes**

Directly instantiating a `Pet`, calling `AssignToOrganization(orgId)`, and saving via `_factory.CreateDbContext()` appears in `AdoptionRequestsControllerTests`, `ChatControllerTests`, `OrganizationMetricsControllerTests`, and `OrgDashboardControllerTests`.

**7. `InitializeAsync` no-op boilerplate repeated in every PetService integration test class**

Every PetService integration test class (~12) contains the same four-line setup block followed by `await Task.CompletedTask;`. The no-op await is semantically meaningless and is copy-pasted identically everywhere.

## Dead Test Code

- Every `InitializeAsync` containing only `await Task.CompletedTask;` (~12 methods) — the `async Task` method can simply return without this statement.

- `tests/PetService/.../Tests/PetFilterTests.cs:76–86` — The `CreatePet` helper discards the HTTP response entirely. If pet creation fails during `SeedTestData`, no assertion fires and all downstream filter tests will assert against an empty DB, either vacuously passing or producing confusing failures.

- `tests/PetService/.../Services/PetRankingServiceTests.cs:37–42` — `CreatePet(Guid id, …)` helper is defined but never called; every test creates pets inline with `Pet.Create(…)`. Unused dead code.

## Bad Test Practices

**1. Missing `// Arrange`, `// Act`, `// Assert` comments**

Project conventions require explicit AAA comments in all test methods. Missing or incorrect in:
- `tests/PetService/.../Domain/AdoptionRequestTests.cs:39–58` — parameter-validation tests have `// Act & Assert` but no `// Arrange` label on the setup block.
- `tests/PetService/.../Tests/AnnouncementsControllerTests.cs:71, 88` — uses `// Arrange & Act` combined, which per convention is only appropriate for simple `[Theory]` value-object tests.
- `tests/PetService/.../Domain/PetTests.cs:244` — `CompleteWorkflow_ReserveAdopt_ShouldSucceed` uses `// Act & Assert` with no `// Arrange` label.

**2. Test method naming deviates from `[MethodUnderTest]_[Scenario]_[ExpectedResult]`**

- `tests/PetService/.../Tests/AnnouncementsControllerTests.cs` — Multiple tests use shortened names missing the controller/endpoint prefix: `GetById_Existing_ReturnsOk`, `GetAll_WithAnnouncements_ReturnsList`.
- `tests/PetService/.../Tests/OrgPetsControllerTests.cs` — `Create_WithValidRequest_ShouldCreatePetInOrg`, `GetAll_ShouldReturnOnlyOrgPets` omit the controller context.
- `tests/PetService/.../Services/ChatAuthorizationServiceTests.cs` — All 5 test methods (`Adopter_Allowed_ForOwnRequest`, etc.) omit the method under test (`AuthorizeAsync`) from their names entirely.
- `tests/UserService/.../Commands/RegisterUserCommandHandlerTests.cs:151, 177` — `HandleAsync_ShouldHashPassword` and `HandleAsync_ShouldCheckEmailExistence` lack a scenario segment.

**3. Misleading file names for both `SqlServerFixture` files**

`tests/PetService/.../Infrastructure/MongoDbFixture.cs` and `tests/UserService/.../Infrastructure/MongoDbFixture.cs` contain zero MongoDB code. Both files define `SqlServerFixture`. The name is actively misleading.

**4. Hard-coded JWT secret duplicated inside `PetServiceWebAppFactory`**

`tests/PetService/.../Infrastructure/PetServiceWebAppFactory.cs:26` defines `private const string TestJwtSecret = "test-secret-key-minimum-32-characters-long-for-testing!"`. The same string literal is repeated verbatim inside `GenerateTestToken` instead of referencing `TestJwtSecret`.

**5. Workflow tests mix multiple Act/Assert blocks without step labeling**

`tests/PetService/.../Tests/PetWorkflowTests.cs` — `FullAdoptionWorkflow_CreateReserveAdopt_Success` and `ReserveCancelReReserveAdopt_Success` each perform three or four state transitions with inline assertions but no `// Act` / `// Assert` step labels.

**6. `OrganizationManagementTests.InitializeAsync` is 77 lines of repeated setup logic**

`tests/UserService/.../Tests/OrganizationManagementTests.cs:41–118` performs five sequential register/promote/login cycles, nearly identical to `AdminUserManagementTests.InitializeAsync:40–93`.

**7. `GetOrgDashboardQueryHandlerTests.cs` defines two test classes in one file**

`tests/PetService/.../Services/GetOrgDashboardQueryHandlerTests.cs` contains both `GetOrgDashboardQueryHandlerTests` and `GetOrgDashboardTrendsQueryHandlerTests`. Project convention is one class per file.

**8. UserService unit test namespace mismatch**

All UserService unit test files use namespace `PetAdoption.UserService.Tests.*` while the assembly is named `PetAdoption.UserService.UnitTests`. Causes IDE navigation confusion.

## Cross-Service Duplication (PetService vs UserService tests)

**1. `SqlServerFixture` / `SqlServerCollection` — identical files**
- `tests/PetService/.../Infrastructure/MongoDbFixture.cs`
- `tests/UserService/.../Infrastructure/MongoDbFixture.cs`

Complete duplication. Any change to the SQL Server image tag or builder config must be made in two places.

**2. `WebApplicationFactory` structure — same template in both services**

Both factories share: constructor pattern, `TestConnectionString` property using `SqlConnectionStringBuilder`, `ConfigureWebHost` removing `DbContextOptions<T>` and all `IHostedService` registrations, `CreateDbContext()`, and `DisposeAsync()` calling `EnsureDeletedAsync`. The only differences are the `DbContext` type and JWT setup in the PetService factory.

**3. Integration test class lifecycle boilerplate — same five-field pattern in both services**

Every integration test class in both services follows the same `_sqlFixture`, `_factory`, `_client` field pattern, same constructor injection, same `InitializeAsync`/`DisposeAsync` shape. An abstract base class `IntegrationTestBase<TFactory>` per service project could collapse this to zero boilerplate.

## Refactoring Opportunities

- **Extract `PetServiceIntegrationTestBase` abstract base class** — hold `_sqlFixture`, `_factory`, `_client`; implement `DisposeAsync`; provide `SetupAsync(role, additionalClaims)`. All 12+ test classes become ~20 lines shorter.

- **Promote `SeedPetTypeAsync` to the base class or a `PetTestHelpers` static class** — single implementation. Overloads: `SeedPetTypeAsync()` for the default "dog"/"Dog" pair, `SeedPetTypeAsync(string code, string name)` for custom types.

- **Rename `MongoDbFixture.cs` to `SqlServerFixture.cs`** in both services.

- **Merge `CreateOrgPetRequestBuilder` into `CreatePetRequestBuilder`** or have them share a common abstract base.

- **Centralize private response DTO records into a `TestDtos.cs` class** per service — removes 5–10 copies of each DTO.

- **Extract `RegisterAndPromoteAsync` helper in UserService tests** — the three-step pattern "register via API → promote role via raw SQL → login to get JWT" is duplicated in `OrganizationManagementTests` and `AdminUserManagementTests`.

- **Remove `await Task.CompletedTask` from all `InitializeAsync` methods** — either remove `async` from methods that don't await, or perform real async setup there.

- **Split `GetOrgDashboardQueryHandlerTests.cs` into two files** — one class per file per project convention.

- **Fix the `TestJwtSecret` constant/literal duplication** in `PetServiceWebAppFactory` — line inside `GenerateTestToken` should reference `TestJwtSecret`.

- **Update UserService unit test namespaces** from `PetAdoption.UserService.Tests.*` to `PetAdoption.UserService.UnitTests.*` to match the assembly name.
