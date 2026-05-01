# User Pet Filters Implementation Plan [COMPLETED]

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add user-facing filters for pet discovery: pet type, age range, breed search, and vaccination status (future-proofed). Users can narrow down which pets appear in the Discover feed.

**Architecture:** Extend the existing `GetPetsQuery` with additional filter parameters (min/max age, breed search). Update `IPetQueryStore.GetFiltered` to accept and apply these filters via EF Core LINQ. The Discover page gets an expandable filter panel with dropdowns, range inputs, and text search. The `/api/pets` endpoint already accepts `petTypeId` — this plan adds `minAge`, `maxAge`, and `breedSearch` query parameters.

**Tech Stack:** .NET 9.0 (PetService), EF Core + SQL Server, custom mediator, Blazor WASM + MudBlazor 8.x, xUnit + FluentAssertions + Testcontainers

**Dependencies:** None — builds on existing GetPetsQuery infrastructure.

---

## File Structure

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs` — Add filter params to query record + handler
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs` — Extend GetFiltered signature
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs` — Implement new filters
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs` — Add query parameters to GET endpoint
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` — Update GetPetsAsync with new params
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor` — Add filter panel UI

### New files:
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetFilterTests.cs` — Integration tests for filters

---

## Chunk 1: Backend — Extend Query Infrastructure

### Task 1.1: Extend GetPetsQuery with filter parameters

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs`

- [ ] **Step 1: Read the current GetPetsQuery**

Read `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs`.

Current signature:
```csharp
public record GetPetsQuery(
    PetStatus? Status,
    Guid? PetTypeId,
    int Skip = 0,
    int Take = 20) : IRequest<GetPetsResponse>;
```

- [ ] **Step 2: Add new filter parameters**

Update the query record:

```csharp
public record GetPetsQuery(
    PetStatus? Status,
    Guid? PetTypeId,
    int Skip = 0,
    int Take = 20,
    int? MinAgeMonths = null,
    int? MaxAgeMonths = null,
    string? BreedSearch = null) : IRequest<GetPetsResponse>;
```

- [ ] **Step 3: Update the handler to pass filters through**

In `GetPetsQueryHandler.Handle`, update the call to `_queryStore.GetFiltered`:

```csharp
var (pets, total) = await _queryStore.GetFiltered(
    request.Status,
    request.PetTypeId,
    request.Skip,
    request.Take,
    request.MinAgeMonths,
    request.MaxAgeMonths,
    request.BreedSearch);
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: Build errors — `IPetQueryStore.GetFiltered` doesn't accept new params yet. That's expected.

- [ ] **Step 5: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/GetPetsQuery.cs
git commit -m "feat: extend GetPetsQuery with age and breed filter parameters"
```

### Task 1.2: Extend IPetQueryStore interface

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs`

- [ ] **Step 1: Read the current interface**

Read `src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs`.

Current signature:
```csharp
Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take);
```

- [ ] **Step 2: Add new parameters to the method**

```csharp
Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take,
    int? minAgeMonths = null,
    int? maxAgeMonths = null,
    string? breedSearch = null);
```

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/IPetQueryStore.cs
git commit -m "feat: extend IPetQueryStore.GetFiltered with filter parameters"
```

### Task 1.3: Implement filters in PetQueryStore

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`

- [ ] **Step 1: Read the current PetQueryStore**

Read `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs`.

- [ ] **Step 2: Update GetFiltered to apply new filters**

Update the implementation to handle the new parameters:

```csharp
public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
    PetStatus? status,
    Guid? petTypeId,
    int skip,
    int take,
    int? minAgeMonths = null,
    int? maxAgeMonths = null,
    string? breedSearch = null)
{
    var query = _context.Pets.AsNoTracking().AsQueryable();

    if (status.HasValue)
        query = query.Where(p => p.Status == status.Value);

    if (petTypeId.HasValue)
        query = query.Where(p => p.PetTypeId == petTypeId.Value);

    if (minAgeMonths.HasValue)
        query = query.Where(p => p.Age != null && p.Age.Months >= minAgeMonths.Value);

    if (maxAgeMonths.HasValue)
        query = query.Where(p => p.Age != null && p.Age.Months <= maxAgeMonths.Value);

    if (!string.IsNullOrWhiteSpace(breedSearch))
        query = query.Where(p => p.Breed != null && p.Breed.Value.Contains(breedSearch.Trim()));

    var total = await query.LongCountAsync();
    var pets = await query.OrderBy(p => p.Name).Skip(skip).Take(take).ToListAsync();

    return (pets, total);
}
```

**Important notes on EF Core + value objects:**
- `p.Age.Months` works if `PetAge` is mapped with `HasConversion<int>` — EF Core translates this to the underlying column.
- `p.Breed.Value.Contains(breedSearch)` works if `PetBreed` is mapped with `HasConversion<string>`.
- If the value object properties can't be accessed in LINQ (check by running the query), you may need to use the raw column name approach. Read the entity configuration first to verify how these value objects are mapped.
- EF Core with `HasConversion` typically converts `p.Breed` to the underlying type transparently. Test to be sure.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/PetQueryStore.cs
git commit -m "feat: implement age and breed filters in PetQueryStore"
```

### Task 1.4: Update PetsController with query parameters

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`

- [ ] **Step 1: Read the current PetsController**

Read `src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs`.

- [ ] **Step 2: Add query parameters to the GET endpoint**

Find the GET `/api/pets` action and add the new parameters:

```csharp
[HttpGet]
[AllowAnonymous]
public async Task<IActionResult> GetPets(
    [FromQuery] string? status = null,
    [FromQuery] Guid? petTypeId = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20,
    [FromQuery] int? minAge = null,
    [FromQuery] int? maxAge = null,
    [FromQuery] string? breed = null)
{
    PetStatus? petStatus = status is not null
        ? Enum.Parse<PetStatus>(status, ignoreCase: true)
        : null;

    var result = await _mediator.Send(new GetPetsQuery(
        petStatus, petTypeId, skip, take, minAge, maxAge, breed));
    return Ok(result);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/PetsController.cs
git commit -m "feat: add age and breed filter query params to PetsController"
```

---

## Chunk 2: Integration Tests

### Task 2.1: Write integration tests for pet filters

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetFilterTests.cs`

- [ ] **Step 1: Read existing integration test setup**

Read the test infrastructure files (factory, fixture, helpers) to understand the patterns.

- [ ] **Step 2: Write the tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PetFilterTests : IAsyncLifetime
{
    private readonly PetServiceWebAppFactory _factory;
    private HttpClient _adminClient = null!;
    private HttpClient _anonClient = null!;
    private Guid _dogTypeId;
    private Guid _catTypeId;

    public PetFilterTests(SqlServerFixture fixture)
    {
        _factory = new PetServiceWebAppFactory(fixture, nameof(PetFilterTests));
    }

    public async Task InitializeAsync()
    {
        _adminClient = _factory.CreateAuthenticatedClient(role: "Admin");
        _anonClient = _factory.CreateClient();
        await SeedTestData();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedTestData()
    {
        // Get or create pet types
        var typesResponse = await _adminClient.GetFromJsonAsync<PetTypesResult>("api/admin/pet-types");
        _dogTypeId = typesResponse!.Items.First(t => t.Code == "dog").Id;
        _catTypeId = typesResponse!.Items.First(t => t.Code == "cat").Id;

        // Create pets with various ages and breeds
        await CreatePet("Buddy", _dogTypeId, "Golden Retriever", 24);
        await CreatePet("Max", _dogTypeId, "German Shepherd", 36);
        await CreatePet("Whiskers", _catTypeId, "Siamese", 12);
        await CreatePet("Luna", _catTypeId, "Persian", 6);
        await CreatePet("Rocky", _dogTypeId, "Bulldog", 48);
    }

    private async Task CreatePet(string name, Guid typeId, string breed, int ageMonths)
    {
        await _adminClient.PostAsJsonAsync("api/pets", new
        {
            Name = name,
            PetTypeId = typeId,
            Breed = breed,
            AgeMonths = ageMonths
        });
    }

    // ──────────────────────────────────────────────────────────────
    // Age Filters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithMinAge_FiltersCorrectly()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>("api/pets?minAge=24");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().OnlyContain(p => p.AgeMonths >= 24);
    }

    [Fact]
    public async Task GetPets_WithMaxAge_FiltersCorrectly()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>("api/pets?maxAge=12");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().OnlyContain(p => p.AgeMonths <= 12);
    }

    [Fact]
    public async Task GetPets_WithAgeRange_FiltersCorrectly()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>("api/pets?minAge=12&maxAge=36");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().OnlyContain(p => p.AgeMonths >= 12 && p.AgeMonths <= 36);
    }

    // ──────────────────────────────────────────────────────────────
    // Breed Search
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithBreedSearch_FiltersCorrectly()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>("api/pets?breed=Golden");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().OnlyContain(p => p.Breed != null && p.Breed.Contains("Golden"));
    }

    [Fact]
    public async Task GetPets_WithBreedSearchNoMatch_ReturnsEmpty()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>("api/pets?breed=Nonexistent");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Combined Filters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPets_WithPetTypeAndAge_CombinesFilters()
    {
        // Act
        var response = await _anonClient.GetFromJsonAsync<PetsResult>(
            $"api/pets?petTypeId={_dogTypeId}&minAge=30");

        // Assert
        response.Should().NotBeNull();
        response!.Pets.Should().AllSatisfy(p =>
        {
            p.Type.Should().Be("Dog");
            p.AgeMonths.Should().BeGreaterThanOrEqualTo(30);
        });
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record PetsResult(List<PetItem> Pets, long Total);
    private record PetItem(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths);
    private record PetTypesResult(List<PetTypeItem> Items);
    private record PetTypeItem(Guid Id, string Code, string Name, bool IsActive);
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~PetFilterTests" -v n`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/PetFilterTests.cs
git commit -m "feat: add pet filter integration tests"
```

---

## Chunk 3: Blazor Frontend — Filter Panel on Discover Page

### Task 3.1: Update PetApiClient with filter parameters

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`

- [ ] **Step 1: Read the current file**

Read `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`.

Current signature:
```csharp
public async Task<PetsResponse?> GetPetsAsync(
    string? status = null, Guid? petTypeId = null, int skip = 0, int take = 20)
```

- [ ] **Step 2: Add filter parameters**

Update the method:

```csharp
public async Task<PetsResponse?> GetPetsAsync(
    string? status = null, Guid? petTypeId = null, int skip = 0, int take = 20,
    int? minAge = null, int? maxAge = null, string? breed = null)
{
    var query = $"api/pets?skip={skip}&take={take}";
    if (status is not null) query += $"&status={status}";
    if (petTypeId.HasValue) query += $"&petTypeId={petTypeId}";
    if (minAge.HasValue) query += $"&minAge={minAge}";
    if (maxAge.HasValue) query += $"&maxAge={maxAge}";
    if (!string.IsNullOrWhiteSpace(breed)) query += $"&breed={Uri.EscapeDataString(breed)}";
    return await _http.GetFromJsonAsync<PetsResponse>(query);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs
git commit -m "feat: add filter parameters to PetApiClient.GetPetsAsync"
```

### Task 3.2: Add filter panel to Discover page

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor`

- [ ] **Step 1: Read the current Discover.razor**

Read `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor`.

- [ ] **Step 2: Replace the simple pet type filter with a full filter panel**

Replace the `<MudSelect>` for pet type with an expandable filter panel:

```razor
@page "/discover"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject ISnackbar Snackbar

<PageTitle>Discover Pets</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">

    @* Filter Panel *@
    <MudExpansionPanels Class="mb-4">
        <MudExpansionPanel Text="Filters" IsInitiallyExpanded="false">
            <MudGrid>
                <MudItem xs="12" sm="6">
                    <MudSelect T="Guid?" Label="Pet Type" @bind-Value="_selectedTypeId" Clearable="true">
                        @if (_petTypes is not null)
                        {
                            @foreach (var pt in _petTypes)
                            {
                                <MudSelectItem Value="@((Guid?)pt.Id)">@pt.Name</MudSelectItem>
                            }
                        }
                    </MudSelect>
                </MudItem>
                <MudItem xs="6" sm="3">
                    <MudNumericField T="int?" Label="Min Age (months)" @bind-Value="_minAge" Min="0" />
                </MudItem>
                <MudItem xs="6" sm="3">
                    <MudNumericField T="int?" Label="Max Age (months)" @bind-Value="_maxAge" Min="0" />
                </MudItem>
                <MudItem xs="12" sm="6">
                    <MudTextField T="string?" Label="Breed" @bind-Value="_breedSearch"
                        Placeholder="e.g. Golden Retriever" Clearable="true" />
                </MudItem>
                <MudItem xs="12" sm="6" Class="d-flex align-end">
                    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="ApplyFilters" Class="mr-2">
                        Apply Filters
                    </MudButton>
                    <MudButton Variant="Variant.Text" OnClick="ClearFilters">Clear</MudButton>
                </MudItem>
            </MudGrid>
        </MudExpansionPanel>
    </MudExpansionPanels>

    @if (_loading)
    {
        <div class="d-flex justify-center mt-8">
            <MudSkeleton SkeletonType="SkeletonType.Rectangle" Width="300px" Height="420px" />
        </div>
    }
    else if (!_pets.Any())
    {
        <MudText Typo="Typo.h6" Align="Align.Center" Color="Color.Secondary" Class="mt-8">
            No more pets to discover. Try adjusting your filters!
        </MudText>
    }
    else
    {
        <div class="d-flex justify-center">
            <SwipeCardStack Pets="_pets" OnFavorite="HandleFavorite" OnSkip="HandleSkip" OnNeedMore="LoadMore" />
        </div>
    }
</MudContainer>

@code {
    private List<PetListItem> _pets = [];
    private List<PetTypeItem>? _petTypes;
    private bool _loading = true;
    private int _skip;
    private bool _hasMore = true;

    // Filter state
    private Guid? _selectedTypeId;
    private int? _minAge;
    private int? _maxAge;
    private string? _breedSearch;

    protected override async Task OnInitializedAsync()
    {
        await LoadPetTypes();
        await LoadPets();
    }

    private async Task LoadPetTypes()
    {
        try
        {
            var response = await PetApi.GetPetTypesAsync();
            _petTypes = response?.Items?.Where(pt => pt.IsActive).ToList();
        }
        catch { }
    }

    private async Task LoadPets()
    {
        _loading = true;
        _skip = 0;
        _pets.Clear();
        _hasMore = true;
        await FetchBatch();
        _loading = false;
    }

    private async Task FetchBatch()
    {
        if (!_hasMore) return;
        try
        {
            var response = await PetApi.GetPetsAsync(
                status: "Available",
                petTypeId: _selectedTypeId,
                skip: _skip,
                take: 10,
                minAge: _minAge,
                maxAge: _maxAge,
                breed: _breedSearch);
            if (response?.Pets is not null)
            {
                var newPets = response.Pets.ToList();
                _pets.AddRange(newPets);
                _skip += newPets.Count;
                _hasMore = newPets.Count == 10;
            }
        }
        catch
        {
            Snackbar.Add("Failed to load pets", Severity.Error);
        }
    }

    private async Task ApplyFilters() => await LoadPets();

    private async Task ClearFilters()
    {
        _selectedTypeId = null;
        _minAge = null;
        _maxAge = null;
        _breedSearch = null;
        await LoadPets();
    }

    private async Task HandleFavorite(PetListItem pet)
    {
        try
        {
            var response = await PetApi.AddFavoriteAsync(pet.Id);
            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Failed to add favorite", Severity.Error);
            }
        }
        catch
        {
            Snackbar.Add("Connection error", Severity.Error);
        }
    }

    private Task HandleSkip(PetListItem _) => Task.CompletedTask;

    private async Task LoadMore() => await FetchBatch();
}
```

**Key changes:**
- Replaced simple `MudSelect` with `MudExpansionPanel` containing full filter form
- Added `MudNumericField` for min/max age
- Added `MudTextField` for breed search
- "Apply Filters" button reloads the pet list
- "Clear" button resets all filters
- Filters are passed through to `PetApi.GetPetsAsync`

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor
git commit -m "feat: add filter panel with age, breed, pet type to Discover page"
```
