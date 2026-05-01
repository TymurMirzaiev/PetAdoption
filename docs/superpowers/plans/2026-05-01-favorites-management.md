# Favorites Management Implementation Plan [COMPLETED]

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enhance the existing favorites system with pagination, search/filter within favorites, sorting options, confirmation dialog for removal, and ability to unlike from pet detail view.

**Architecture:** The backend already has favorites CRUD (add, remove, list). This plan enhances the frontend Favorites page with better UX (confirmation dialogs, sorting, filtering) and extends the backend GetFavorites query with sorting and pet type filter support. The existing PetDetail page (if present) gets an unlike/favorite toggle button.

**Tech Stack:** .NET 9.0 (PetService), EF Core + SQL Server, custom mediator, Blazor WASM + MudBlazor 8.x, xUnit + FluentAssertions + Testcontainers

**Dependencies:** None — the favorites system already exists. This plan is purely enhancements.

---

## File Structure

### Modified files:
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs` — Add sorting and pet type filter parameters
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQueryHandler.cs` — Handle new parameters
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs` — Update interface for new parameters
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs` — Implement sorting and filtering
- `src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs` — Add query parameters
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor` — Full UX overhaul
- `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs` — Update GetFavoritesAsync signature
- `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` — Update FavoritesResponse if needed

### New files:
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/CheckFavoriteQuery.cs` — Check if user has favorited a pet
- `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/FavoritesEnhancedTests.cs` — Integration tests for new features

### Possibly modified:
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/PetDetail.razor` — Add favorite toggle (if this page exists; check first)

---

## Chunk 1: Backend — Enhanced GetFavorites Query

### Task 1.1: Read existing favorites query infrastructure

**Files to read (do NOT modify yet):**
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs`
- `src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs`
- `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs`

- [ ] **Step 1: Read all three files to understand current implementation**

Read each file and note the current method signatures and query patterns.

### Task 1.2: Add sorting and filtering to GetFavoritesQuery

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs`

- [ ] **Step 1: Read the current GetFavoritesQuery file**

Read `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs`.

- [ ] **Step 2: Update the query record to include new parameters**

Update the query record to add sorting, pet type filter, and status filter:

```csharp
public record GetFavoritesQuery(
    Guid UserId,
    int Skip,
    int Take,
    Guid? PetTypeId = null,
    string? PetStatus = null,
    string SortBy = "newest") : IRequest<GetFavoritesResponse>;
```

`SortBy` values: `"newest"` (default, by CreatedAt desc), `"oldest"` (CreatedAt asc), `"name"` (PetName asc).

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: May have build errors if handler references changed. That's expected — we'll fix in next task.

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQuery.cs
git commit -m "feat: add sorting and filtering params to GetFavoritesQuery"
```

### Task 1.3: Update IFavoriteQueryStore interface

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs`

- [ ] **Step 1: Read the current interface**

Read `src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs`.

- [ ] **Step 2: Update the interface**

The interface method that returns favorites should accept the new parameters. Update it:

```csharp
Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetUserFavorites(
    Guid userId, int skip, int take,
    Guid? petTypeId = null, string? petStatus = null, string sortBy = "newest");
```

If the interface currently uses a different return type (returning raw Favorite entities), add a new DTO:

```csharp
public record FavoriteWithPetDto(
    Guid FavoriteId, Guid PetId, string PetName, Guid PetTypeId,
    string? Breed, int? AgeMonths, string Status, DateTime FavoritedAt);
```

**Note:** Read the existing code first. If the query store already returns enriched data, just add the filter parameters to the existing method.

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/IFavoriteQueryStore.cs
git commit -m "feat: update IFavoriteQueryStore with filter and sort params"
```

### Task 1.4: Update FavoriteQueryStore implementation

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs`

- [ ] **Step 1: Read the current implementation**

Read `src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs`.

- [ ] **Step 2: Implement sorting and filtering**

Update the query store implementation to support the new parameters. The implementation should:

1. Join Favorites with Pets to get pet data
2. Filter by PetTypeId if provided
3. Filter by PetStatus if provided (parse string to PetStatus enum)
4. Sort by the specified field:
   - `"newest"` → `OrderByDescending(f => f.CreatedAt)`
   - `"oldest"` → `OrderBy(f => f.CreatedAt)`
   - `"name"` → `OrderBy(p => p.Name)` (join with Pet)

Example implementation pattern:

```csharp
public async Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetUserFavorites(
    Guid userId, int skip, int take,
    Guid? petTypeId = null, string? petStatus = null, string sortBy = "newest")
{
    var query = from f in _context.Favorites.AsNoTracking()
                join p in _context.Pets.AsNoTracking() on f.PetId equals p.Id
                where f.UserId == userId
                select new { Favorite = f, Pet = p };

    if (petTypeId.HasValue)
        query = query.Where(x => x.Pet.PetTypeId == petTypeId.Value);

    if (petStatus is not null && Enum.TryParse<PetStatus>(petStatus, true, out var status))
        query = query.Where(x => x.Pet.Status == status);

    var total = await query.LongCountAsync();

    query = sortBy switch
    {
        "oldest" => query.OrderBy(x => x.Favorite.CreatedAt),
        "name" => query.OrderBy(x => x.Pet.Name),
        _ => query.OrderByDescending(x => x.Favorite.CreatedAt)
    };

    var items = await query.Skip(skip).Take(take)
        .Select(x => new FavoriteWithPetDto(
            x.Favorite.Id, x.Pet.Id, x.Pet.Name.Value, x.Pet.PetTypeId,
            x.Pet.Breed != null ? x.Pet.Breed.Value : null,
            x.Pet.Age != null ? x.Pet.Age.Months : (int?)null,
            x.Pet.Status.ToString(),
            x.Favorite.CreatedAt))
        .ToListAsync();

    return (items, total);
}
```

**Important:** Read the existing query store first. The exact LINQ shape depends on how value objects are mapped. EF Core's `HasConversion` may require adjusting property access (e.g., `p.Name` might need `.Value` only in the projection, not in the LINQ where clause). Test and adjust.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Infrastructure`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/FavoriteQueryStore.cs
git commit -m "feat: implement sorting and filtering in FavoriteQueryStore"
```

### Task 1.5: Update GetFavoritesQueryHandler

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.Application/Queries/GetFavoritesQueryHandler.cs` (or wherever the handler is defined)

- [ ] **Step 1: Read the current handler**

Read the file that contains the GetFavoritesQueryHandler.

- [ ] **Step 2: Pass new parameters through to the query store**

Update the handler to pass `PetTypeId`, `PetStatus`, and `SortBy` from the query to the query store call:

```csharp
var (items, total) = await _queryStore.GetUserFavorites(
    request.UserId, request.Skip, request.Take,
    request.PetTypeId, request.PetStatus, request.SortBy);
```

Adjust the response DTO mapping as needed based on what `FavoriteWithPetDto` returns vs what the existing `GetFavoritesResponse` expects.

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/
git commit -m "feat: update GetFavoritesQueryHandler with sorting and filtering"
```

### Task 1.6: Update FavoritesController with query parameters

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs`

- [ ] **Step 1: Read the current controller**

Read `src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs`.

- [ ] **Step 2: Add query parameters to GetFavorites endpoint**

Update the `GetFavorites` action:

```csharp
[HttpGet]
public async Task<IActionResult> GetFavorites(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 10,
    [FromQuery] Guid? petTypeId = null,
    [FromQuery] string? status = null,
    [FromQuery] string sortBy = "newest")
{
    var result = await _mediator.Send(new GetFavoritesQuery(
        GetUserId(), skip, take, petTypeId, status, sortBy));
    return Ok(result);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs
git commit -m "feat: add sorting and filtering query params to FavoritesController"
```

---

## Chunk 2: Backend — Check Favorite Query

### Task 2.1: Add CheckFavoriteQuery (for favorite toggle on pet detail)

**Files:**
- Create: `src/Services/PetService/PetAdoption.PetService.Application/Queries/CheckFavoriteQuery.cs`

- [ ] **Step 1: Create the query and handler**

```csharp
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record CheckFavoriteQuery(Guid UserId, Guid PetId) : IRequest<CheckFavoriteResponse>;

public record CheckFavoriteResponse(bool IsFavorited);

public class CheckFavoriteQueryHandler : IRequestHandler<CheckFavoriteQuery, CheckFavoriteResponse>
{
    private readonly IFavoriteRepository _repository;

    public CheckFavoriteQueryHandler(IFavoriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckFavoriteResponse> Handle(
        CheckFavoriteQuery request, CancellationToken cancellationToken = default)
    {
        var exists = await _repository.ExistsByUserAndPetAsync(request.UserId, request.PetId, cancellationToken);
        return new CheckFavoriteResponse(exists);
    }
}
```

**Note:** This requires `ExistsByUserAndPetAsync` on `IFavoriteRepository`. Read the existing repository interface first. If this method doesn't exist, add it:

```csharp
// Add to IFavoriteRepository:
Task<bool> ExistsByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default);
```

And implement in the repository:

```csharp
public async Task<bool> ExistsByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default)
{
    return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.PetId == petId, ct);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.Application`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.Application/Queries/CheckFavoriteQuery.cs src/Services/PetService/PetAdoption.PetService.Domain/Interfaces/ src/Services/PetService/PetAdoption.PetService.Infrastructure/Persistence/
git commit -m "feat: add CheckFavoriteQuery for favorite toggle"
```

### Task 2.2: Add check-favorite endpoint to FavoritesController

**Files:**
- Modify: `src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs`

- [ ] **Step 1: Add the endpoint**

```csharp
[HttpGet("check/{petId:guid}")]
public async Task<IActionResult> CheckFavorite(Guid petId)
{
    var result = await _mediator.Send(new CheckFavoriteQuery(GetUserId(), petId));
    return Ok(result);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Services/PetService/PetAdoption.PetService.API`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Services/PetService/PetAdoption.PetService.API/Controllers/FavoritesController.cs
git commit -m "feat: add check-favorite endpoint"
```

---

## Chunk 3: Integration Tests

### Task 3.1: Write integration tests for enhanced favorites

**Files:**
- Create: `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/FavoritesEnhancedTests.cs`

- [ ] **Step 1: Read existing favorites integration tests**

Read `tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/` to find any existing favorites tests and understand the test setup.

- [ ] **Step 2: Write the tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class FavoritesEnhancedTests : IAsyncLifetime
{
    private readonly PetServiceWebAppFactory _factory;
    private HttpClient _client = null!;

    public FavoritesEnhancedTests(SqlServerFixture fixture)
    {
        _factory = new PetServiceWebAppFactory(fixture, nameof(FavoritesEnhancedTests));
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateAuthenticatedClient();
        await SeedTestData();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedTestData()
    {
        // Seed pets and add favorites — adapt based on test infrastructure
        // Create admin client to create pets, then use user client to favorite them
    }

    // ──────────────────────────────────────────────────────────────
    // Sorting
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("newest")]
    [InlineData("oldest")]
    [InlineData("name")]
    public async Task GetFavorites_WithSortBy_ReturnsOrderedResults(string sortBy)
    {
        // Act
        var response = await _client.GetAsync($"api/favorites?sortBy={sortBy}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────
    // Check Favorite
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckFavorite_WhenFavorited_ReturnsTrue()
    {
        // Arrange — add a favorite first
        // var petId = ... (from seed data)
        // await _client.PostAsJsonAsync("api/favorites", new { PetId = petId });

        // Act
        // var response = await _client.GetFromJsonAsync<CheckFavoriteResult>($"api/favorites/check/{petId}");

        // Assert
        // response!.IsFavorited.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFavorite_WhenNotFavorited_ReturnsFalse()
    {
        // Arrange
        var randomPetId = Guid.NewGuid();

        // Act
        var response = await _client.GetFromJsonAsync<CheckFavoriteResult>($"api/favorites/check/{randomPetId}");

        // Assert
        response!.IsFavorited.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CheckFavoriteResult(bool IsFavorited);
}
```

**Note:** These are scaffolds. Flesh out seed data and assertions based on the actual test infrastructure.

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/PetService/PetAdoption.PetService.IntegrationTests --filter "FullyQualifiedName~FavoritesEnhancedTests" -v n`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/PetService/PetAdoption.PetService.IntegrationTests/Tests/FavoritesEnhancedTests.cs
git commit -m "feat: add favorites enhancement integration tests"
```

---

## Chunk 4: Blazor Frontend — Enhanced Favorites Page

### Task 4.1: Update PetApiClient for enhanced favorites

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`

- [ ] **Step 1: Read the current file**

Read `src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs`.

- [ ] **Step 2: Update GetFavoritesAsync and add CheckFavoriteAsync**

Update the existing method signature and add the check method:

```csharp
    public async Task<FavoritesResponse?> GetFavoritesAsync(
        int skip = 0, int take = 10,
        Guid? petTypeId = null, string? status = null, string sortBy = "newest")
    {
        var query = $"api/favorites?skip={skip}&take={take}&sortBy={sortBy}";
        if (petTypeId.HasValue) query += $"&petTypeId={petTypeId}";
        if (status is not null) query += $"&status={status}";
        return await _http.GetFromJsonAsync<FavoritesResponse>(query);
    }

    public async Task<bool> CheckFavoriteAsync(Guid petId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<CheckFavoriteResponse>($"api/favorites/check/{petId}");
            return result?.IsFavorited ?? false;
        }
        catch { return false; }
    }
```

- [ ] **Step 3: Add CheckFavoriteResponse to ApiModels.cs**

Read `src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs` and add:

```csharp
public record CheckFavoriteResponse(bool IsFavorited);
```

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Services/PetApiClient.cs src/Web/PetAdoption.Web.BlazorApp/Models/ApiModels.cs
git commit -m "feat: update PetApiClient with enhanced favorites methods"
```

### Task 4.2: Rewrite Favorites.razor with enhanced UX

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor`

- [ ] **Step 1: Read the current Favorites.razor**

Read `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor`.

- [ ] **Step 2: Rewrite with filters, sorting, pagination, and confirmation dialog**

Replace the `@code` block and markup with enhanced version:

```razor
@page "/favorites"
@layout MainLayout
@attribute [Authorize]
@inject PetApiClient PetApi
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<PageTitle>My Favorites</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">My Favorites</MudText>

    @* Filter bar *@
    <MudGrid Class="mb-4">
        <MudItem xs="12" sm="4">
            <MudSelect T="Guid?" Label="Pet Type" @bind-Value="_selectedTypeId" @bind-Value:after="OnFilterChanged" Clearable="true">
                @if (_petTypes is not null)
                {
                    @foreach (var pt in _petTypes)
                    {
                        <MudSelectItem Value="@((Guid?)pt.Id)">@pt.Name</MudSelectItem>
                    }
                }
            </MudSelect>
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudSelect T="string?" Label="Status" @bind-Value="_selectedStatus" @bind-Value:after="OnFilterChanged" Clearable="true">
                <MudSelectItem Value="@("Available")">Available</MudSelectItem>
                <MudSelectItem Value="@("Reserved")">Reserved</MudSelectItem>
                <MudSelectItem Value="@("Adopted")">Adopted</MudSelectItem>
            </MudSelect>
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudSelect T="string" Label="Sort By" @bind-Value="_sortBy" @bind-Value:after="OnFilterChanged">
                <MudSelectItem Value="@("newest")">Newest First</MudSelectItem>
                <MudSelectItem Value="@("oldest")">Oldest First</MudSelectItem>
                <MudSelectItem Value="@("name")">Name (A-Z)</MudSelectItem>
            </MudSelect>
        </MudItem>
    </MudGrid>

    @if (_loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (!_favorites.Any())
    {
        <MudText Typo="Typo.h6" Color="Color.Secondary" Align="Align.Center" Class="mt-8">
            No favorites yet. Start discovering pets!
        </MudText>
        <div class="d-flex justify-center mt-4">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/discover">Discover Pets</MudButton>
        </div>
    }
    else
    {
        <MudGrid>
            @foreach (var fav in _favorites)
            {
                <MudItem xs="12" sm="6" md="4">
                    <MudCard Elevation="3">
                        <MudCardHeader>
                            <CardHeaderAvatar>
                                <MudAvatar Color="Color.Primary">@fav.PetName[..1]</MudAvatar>
                            </CardHeaderAvatar>
                            <CardHeaderContent>
                                <MudText Typo="Typo.h6">@fav.PetName</MudText>
                                <MudText Typo="Typo.body2" Color="Color.Secondary">@fav.PetType</MudText>
                            </CardHeaderContent>
                        </MudCardHeader>
                        <MudCardContent>
                            @if (fav.Breed is not null)
                            {
                                <MudText Typo="Typo.body2"><b>Breed:</b> @fav.Breed</MudText>
                            }
                            @if (fav.AgeMonths.HasValue)
                            {
                                <MudText Typo="Typo.body2"><b>Age:</b> @FormatAge(fav.AgeMonths.Value)</MudText>
                            }
                            <MudChip T="string" Size="Size.Small" Color="@StatusColor(fav.Status)">@fav.Status</MudChip>
                        </MudCardContent>
                        <MudCardActions>
                            <MudButton Size="Size.Small" Color="Color.Primary"
                                OnClick="@(() => Navigation.NavigateTo($"/pets/{fav.PetId}"))">View</MudButton>
                            @if (fav.Status == "Available")
                            {
                                <MudButton Size="Size.Small" Color="Color.Success"
                                    OnClick="@(() => ReservePet(fav.PetId))">Reserve</MudButton>
                            }
                            <MudIconButton Icon="@Icons.Material.Filled.HeartBroken" Size="Size.Small" Color="Color.Error"
                                OnClick="@(() => ConfirmRemoveFavorite(fav.PetId, fav.PetName))" />
                        </MudCardActions>
                    </MudCard>
                </MudItem>
            }
        </MudGrid>

        @* Pagination *@
        @if (_totalCount > _pageSize)
        {
            <div class="d-flex justify-center mt-4">
                <MudPagination Count="@((int)Math.Ceiling((double)_totalCount / _pageSize))"
                    Selected="_currentPage"
                    SelectedChanged="OnPageChanged" />
            </div>
        }
    }
</MudContainer>

@code {
    private List<FavoriteItem> _favorites = [];
    private List<PetTypeItem>? _petTypes;
    private bool _loading = true;
    private long _totalCount;
    private int _pageSize = 12;
    private int _currentPage = 1;
    private Guid? _selectedTypeId;
    private string? _selectedStatus;
    private string _sortBy = "newest";

    protected override async Task OnInitializedAsync()
    {
        await LoadPetTypes();
        await LoadFavorites();
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

    private async Task LoadFavorites()
    {
        _loading = true;
        try
        {
            var skip = (_currentPage - 1) * _pageSize;
            var response = await PetApi.GetFavoritesAsync(
                skip, _pageSize, _selectedTypeId, _selectedStatus, _sortBy);
            _favorites = response?.Items?.ToList() ?? [];
            _totalCount = response?.TotalCount ?? 0;
        }
        catch { Snackbar.Add("Failed to load favorites", Severity.Error); }
        finally { _loading = false; }
    }

    private async Task OnFilterChanged()
    {
        _currentPage = 1;
        await LoadFavorites();
    }

    private async Task OnPageChanged(int page)
    {
        _currentPage = page;
        await LoadFavorites();
    }

    private async Task ConfirmRemoveFavorite(Guid petId, string petName)
    {
        var result = await DialogService.ShowMessageBox(
            "Remove Favorite",
            $"Are you sure you want to remove {petName} from your favorites?",
            yesText: "Remove", cancelText: "Cancel");

        if (result == true)
            await RemoveFavorite(petId);
    }

    private async Task RemoveFavorite(Guid petId)
    {
        try
        {
            var response = await PetApi.RemoveFavoriteAsync(petId);
            if (response.IsSuccessStatusCode)
            {
                _favorites.RemoveAll(f => f.PetId == petId);
                _totalCount--;
                Snackbar.Add("Removed from favorites", Severity.Info);
            }
        }
        catch { Snackbar.Add("Failed to remove favorite", Severity.Error); }
    }

    private async Task ReservePet(Guid petId)
    {
        try
        {
            var response = await PetApi.ReservePetAsync(petId);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Pet reserved!", Severity.Success);
                await LoadFavorites();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                Snackbar.Add(error?.Message ?? "Failed to reserve", Severity.Error);
            }
        }
        catch { Snackbar.Add("Connection error", Severity.Error); }
    }

    private static Color StatusColor(string status) => status switch
    {
        "Available" => Color.Success,
        "Reserved" => Color.Warning,
        "Adopted" => Color.Info,
        _ => Color.Default
    };

    private static string FormatAge(int months) =>
        months >= 12 ? $"{months / 12}y {months % 12}m" : $"{months}m";
}
```

**Key enhancements:**
- Pet type filter dropdown
- Status filter dropdown
- Sort by dropdown (newest, oldest, name)
- Pagination with `MudPagination`
- Confirmation dialog before removing favorites (using `DialogService.ShowMessageBox`)
- Changed delete icon from trash to heart-broken for better UX

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/Favorites.razor
git commit -m "feat: enhance Favorites page with filters, sorting, pagination, confirmation"
```

### Task 4.3: Add favorite toggle to PetDetail page (if it exists)

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/PetDetail.razor` (or wherever pet detail view lives)

- [ ] **Step 1: Check if PetDetail page exists**

Search for a PetDetail page:
- Look for files matching `**/PetDetail.razor`, `**/PetDetails.razor`, or `**/Pet.razor` with `@page "/pets/{id}"` route.

If no pet detail page exists, skip this task.

- [ ] **Step 2: Add favorite toggle button**

If the page exists, add a favorite toggle button in the card actions area:

```razor
@* Add to the inject section: *@
@inject PetApiClient PetApi

@* Add state variables: *@
private bool _isFavorited;

@* In OnInitializedAsync or OnParametersSetAsync: *@
_isFavorited = await PetApi.CheckFavoriteAsync(petId);

@* Add toggle button: *@
<MudIconButton Icon="@(_isFavorited ? Icons.Material.Filled.Favorite : Icons.Material.Filled.FavoriteBorder)"
    Color="@(_isFavorited ? Color.Error : Color.Default)"
    OnClick="ToggleFavorite" />

@* Add handler: *@
private async Task ToggleFavorite()
{
    try
    {
        if (_isFavorited)
        {
            var response = await PetApi.RemoveFavoriteAsync(petId);
            if (response.IsSuccessStatusCode) _isFavorited = false;
        }
        else
        {
            var response = await PetApi.AddFavoriteAsync(petId);
            if (response.IsSuccessStatusCode) _isFavorited = true;
        }
    }
    catch { Snackbar.Add("Failed to update favorite", Severity.Error); }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/
git commit -m "feat: add favorite toggle to pet detail page"
```
