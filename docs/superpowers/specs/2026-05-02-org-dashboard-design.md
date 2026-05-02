# Org Dashboard — Design Spec

**Date:** 2026-05-02  
**Status:** Approved  
**Scope:** PetService backend (2 new query handlers + controller actions) + Blazor WASM frontend (1 new page + nav update)

---

## Overview

A summary landing page for organisation users (shelter staff) at `/org/{OrgId:guid}/dashboard`. Replaces nothing — the existing Pets, Adoption Requests, and OrgMetrics pages remain. The dashboard becomes the first destination when an org user navigates to their organisation.

---

## Architecture

Clean Architecture, CQRS read path only. Two new queries in `PetService`:

```
OrgDashboardController
  ├── GET /api/organizations/{orgId}/dashboard         → GetOrgDashboardQuery
  └── GET /api/organizations/{orgId}/dashboard/trends  → GetOrgDashboardTrendsQuery
```

Both queries are read-only (`IQueryStore`). No writes. No domain events. Auth via the existing `OrgAuthorizationFilter` (`[ServiceFilter(typeof(OrgAuthorizationFilter))]`). Route parameter **must be named `{orgId}`** (not `{id}`) because `OrgAuthorizationFilter` reads `context.RouteData.Values["orgId"]`.

---

## Domain change: `Pet.AdoptedAt`

The trends query groups pets by when they were adopted. The `Pet` entity currently has no adoption timestamp. Add:

- `public DateTime? AdoptedAt { get; private set; }` to `Pet.cs`
- Set it inside `Pet.Adopt()`: `AdoptedAt = DateTime.UtcNow;`
- Map it in `PetEntityConfiguration` (or the existing `PetServiceDbContext` entity config): `builder.Property(p => p.AdoptedAt);`

This is the only domain change required.

---

## Backend

### GetOrgDashboardQuery

**Endpoint:** `GET /api/organizations/{orgId}/dashboard`  
**Auth:** Authenticated + org member  
**Handler:** `GetOrgDashboardQueryHandler` in `PetService.Application/Queries/GetOrgDashboardQuery.cs`  
**Query store method:** `IOrgDashboardQueryStore.GetDashboardAsync(Guid orgId)`

The query store runs four EF Core aggregates. The handler calls them in parallel via `Task.WhenAll`, then computes derived values:

1. Pet counts grouped by status (`Available`, `Reserved`, `Adopted`) — `WHERE OrganizationId == orgId`
2. Adoption request counts grouped by status — `WHERE OrganizationId == orgId`
3. Total impressions: `SUM` of `PetInteractions.Count` for `InteractionType == Impression` joined to org pets
4. Swipe counts: `SUM` of `PetInteractions.Count` for `InteractionType == Swipe` joined to org pets

`AdoptionRate` and `AvgSwipeRate` are **computed in the handler** (not in the query store):
```csharp
var adoptionRate = totalPets == 0 ? 0 : Math.Round(adoptedPets / (double)totalPets * 100, 1);
var avgSwipeRate = totalImpressions == 0 ? 0 : Math.Round(totalSwipes / (double)totalImpressions * 100, 1);
```

Both are **0–100 percentage values** (e.g. `23.4` meaning 23.4 %). The Blazor page displays them with a `%` suffix. This matches the convention used in `PetMetricsQueryStore` where `SwipeRate` is stored as a 0–1 fraction — the dashboard intentionally uses the human-readable 0–100 form to avoid frontend multiplication.

**Response DTO and handler live in the same file** (`GetOrgDashboardQuery.cs`):
```csharp
public record GetOrgDashboardQuery(Guid OrgId);

public record GetOrgDashboardResponse(
    int TotalPets,
    int AvailablePets,
    int ReservedPets,
    int AdoptedPets,
    int TotalAdoptionRequests,
    int PendingRequests,
    double AdoptionRate,        // 0–100, e.g. 23.4 = 23.4%
    long TotalImpressions,
    double AvgSwipeRate         // 0–100, e.g. 18.7 = 18.7%
);
```

---

### GetOrgDashboardTrendsQuery

**Endpoint:** `GET /api/organizations/{orgId}/dashboard/trends?from=&to=`  
**Auth:** Authenticated + org member  
**Handler:** `GetOrgDashboardTrendsQueryHandler` in `GetOrgDashboardTrendsQuery.cs`  
**Query store method:** `IOrgDashboardQueryStore.GetTrendsAsync(Guid orgId, DateTime from, DateTime to)`

**Validation (in handler):**
- `from` defaults to `DateTime.UtcNow.AddDays(-84)` (12 weeks) if not supplied
- `to` defaults to `DateTime.UtcNow` if not supplied
- If `from >= to`, return `400 Bad Request`
- Range clamped to max 52 weeks (364 days); if `to - from > 364 days`, move `from` up to `to.AddDays(-364)`

**Queries (EF Core LINQ, no raw SQL):**

Adoptions by week — uses the new `Pet.AdoptedAt` field:
```csharp
_db.Pets
   .Where(p => p.OrganizationId == orgId
            && p.Status == PetStatus.Adopted
            && p.AdoptedAt >= from && p.AdoptedAt <= to)
   .GroupBy(p => EF.Functions.DateDiffDay(from, p.AdoptedAt!.Value) / 7)
   .Select(g => new { WeekOffset = g.Key, Count = g.Count() })
```

Requests by week:
```csharp
_db.AdoptionRequests
   .Where(r => r.OrganizationId == orgId
            && r.CreatedAt >= from && r.CreatedAt <= to)
   .GroupBy(r => EF.Functions.DateDiffDay(from, r.CreatedAt) / 7)
   .Select(g => new { WeekOffset = g.Key, Count = g.Count() })
```

Both return sparse results (only weeks with activity). The **handler** iterates all week offsets from 0 to `(int)(to - from).TotalDays / 7` and fills in zero for any missing offset, producing a dense list aligned to the same X-axis.

`WeekStart` for offset `n` = `from.AddDays(n * 7)`.  
`Label` = `WeekStart.ToString("MMM d")` (e.g. `"Apr 7"`).

**Response DTO** (in `GetOrgDashboardTrendsQuery.cs`):
```csharp
public record GetOrgDashboardTrendsQuery(Guid OrgId, DateTime? From, DateTime? To);

public record GetOrgDashboardTrendsResponse(
    IReadOnlyList<TrendPoint> AdoptionsByWeek,
    IReadOnlyList<TrendPoint> RequestsByWeek
);

public record TrendPoint(DateTime WeekStart, string Label, int Count);
```

---

### New files (PetService)

| File | Purpose |
|------|---------|
| `Domain/Pet.cs` | Add `AdoptedAt` property + set in `Adopt()` |
| `Application/Queries/GetOrgDashboardQuery.cs` | Query record, response record, handler |
| `Application/Queries/GetOrgDashboardTrendsQuery.cs` | Query record, response record, `TrendPoint`, handler |
| `Application/Queries/IOrgDashboardQueryStore.cs` | Read interface with `GetDashboardAsync` + `GetTrendsAsync` |
| `Infrastructure/Persistence/OrgDashboardQueryStore.cs` | EF Core implementation |
| `API/Controllers/OrgDashboardController.cs` | Two GET actions with `OrgAuthorizationFilter` |

**Registration:** `IOrgDashboardQueryStore` → `OrgDashboardQueryStore` is registered as scoped in `Program.cs` alongside other query store registrations (e.g. `builder.Services.AddScoped<IOrgDashboardQueryStore, OrgDashboardQueryStore>()`). It is **not** registered in `ServiceCollectionExtensions` — that file only registers mediator handlers.

---

## Frontend

### Page: `OrgDashboard.razor`

**Route:** `/org/{OrgId:guid}/dashboard`  
**Layout:** `MainLayout`  
**Auth:** `[Authorize]`  
**File:** `Pages/Organization/OrgDashboard.razor`

**Load sequence:**
1. On init: fire KPI call (`GetOrgDashboardAsync`) and recent-requests call (`GetOrgAdoptionRequestsAsync(OrgId, status: null, take: 10)`) in parallel via `Task.WhenAll`
2. Trends loaded separately (non-blocking `_ = LoadTrendsAsync()`) with default 12-week range so KPIs render immediately
3. Date range change re-fetches only trends

The recent requests section reuses the **existing** `PetApiClient.GetOrgAdoptionRequestsAsync(orgId, statusFilter: null, take: 10)` — no new endpoint needed. Results are sorted descending by `CreatedAt` (the existing endpoint already returns them newest-first).

**Layout (MudBlazor):**

```
MudGrid
  ├── 4 × MudItem xs=12 sm=6 md=3   → KPI cards (MudPaper)
  ├── MudItem xs=12                  → Date range pickers (From/To) + Apply button
  ├── 2 × MudItem xs=12 sm=6        → MudChart Bar — Adoptions by week | Requests by week
  └── MudItem xs=12                  → Recent requests MudTable + "View all →" link
```

**KPI cards** — each `MudPaper` contains:
- `MudIcon` (large, colored)
- Primary number (`Typo.h4`)
- Label (`Typo.body2`, secondary color)
- Sub-detail: Total Pets card shows three small chips (Available / Reserved / Adopted counts); Adoption Rate card shows `"X adopted of Y total"`; AvgSwipeRate card appends `%` suffix

**Bar charts** — `MudChart ChartType="ChartType.Bar"`:
- `ChartSeries` bound to `double[]` of `TrendPoint.Count` values (cast to double for MudChart)
- `XAxisLabels` bound to `string[]` of `TrendPoint.Label` values
- `Height="300"`, `Width="100%"`
- `MudSkeleton SkeletonType.Rectangle Height="300px"` shown while trends are loading

**Recent requests table** — `MudTable` (10 rows, no server paging):
- Columns: Pet name · Status chip · Message preview (truncate to 60 chars + `…`) · Submitted (relative, e.g. `"3 days ago"` using `DateTime.UtcNow - r.CreatedAt`) · Actions (Approve / Reject for Pending rows)
- "View all →" `MudLink` top-right navigating to `/organization/{OrgId}/adoption-requests`
- Approve/Reject call existing `PetApiClient.ApproveAdoptionRequestAsync` / `RejectAdoptionRequestAsync`; on success reload only the 10-row list

**API client additions** (`PetApiClient.cs`):
```csharp
Task<OrgDashboardResponse?> GetOrgDashboardAsync(Guid orgId);
Task<OrgDashboardTrendsResponse?> GetOrgDashboardTrendsAsync(Guid orgId, DateTime? from, DateTime? to);
```

**New model records** (`Models/ApiModels.cs`) — client-side duplicates of the backend DTOs, consistent with all other records in this file:
```csharp
public record OrgDashboardResponse(
    int TotalPets, int AvailablePets, int ReservedPets, int AdoptedPets,
    int TotalAdoptionRequests, int PendingRequests,
    double AdoptionRate, long TotalImpressions, double AvgSwipeRate);
public record OrgDashboardTrendsResponse(
    IReadOnlyList<TrendPoint> AdoptionsByWeek,
    IReadOnlyList<TrendPoint> RequestsByWeek);
// TrendPoint is a client-side model duplicate; backend defines its own in the query file
public record TrendPoint(DateTime WeekStart, string Label, int Count);
```

### Navigation update

In `MainLayout.razor`, the org Dashboard nav link must only appear for users who have an `organizationId` JWT claim (org members). Wrap it in an `<AuthorizeView>` check using the `organizationId` claim:

```razor
@if (!string.IsNullOrEmpty(orgId))
{
    <MudNavLink Href="@($"/org/{orgId}/dashboard")" Icon="@Icons.Material.Filled.Dashboard">
        Dashboard
    </MudNavLink>
}
```

Where `orgId` is read from `AuthenticationState` claims (`User.FindFirst("organizationId")?.Value`). This prevents the link from appearing for regular users (who would get 403 on click).

---

## Error handling

| Scenario | Behaviour |
|----------|-----------|
| KPI fetch fails | `MudAlert Severity.Error` in place of cards; trends and requests still load |
| Trends fetch fails | `MudAlert` inside chart area; KPIs and requests unaffected |
| Requests fetch fails | `MudAlert` inside table area |
| `from >= to` on trends | Handler returns 400; Blazor shows snackbar "End date must be after start date" |
| No data (new org) | Charts render with all-zero series; cards show `0`; table shows "No requests yet" |

---

## Out of scope

- No push/real-time updates
- No export (CSV/PDF)
- No per-pet drill-down from the dashboard (OrgMetrics handles that)
- No changes to existing pages

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `GetOrgDashboardQueryHandlerTests` — mock `IOrgDashboardQueryStore`, verify `AdoptionRate` and `AvgSwipeRate` computed correctly from raw counts (including zero-denominator edge cases)
- `GetOrgDashboardTrendsQueryHandlerTests` — verify date defaulting, 52-week clamping, zero-fill producing dense week list

**Integration tests** (`PetService.IntegrationTests`):
- `OrgDashboardControllerTests` — seed org with pets in `Available`/`Reserved`/`Adopted` statuses + interactions, assert KPI endpoint returns correct counts; seed adoption requests across multiple weeks, assert trends endpoint groups and zero-fills correctly
