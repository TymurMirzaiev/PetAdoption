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
  ├── GET /api/organizations/{id}/dashboard         → GetOrgDashboardQuery
  └── GET /api/organizations/{id}/dashboard/trends  → GetOrgDashboardTrendsQuery
```

Both queries are read-only (`IQueryStore`). No writes. No domain events. Auth via the existing `OrgAuthorizationFilter` (`[ServiceFilter(typeof(OrgAuthorizationFilter))]`).

---

## Backend

### GetOrgDashboardQuery

**Endpoint:** `GET /api/organizations/{id}/dashboard`  
**Auth:** Authenticated + org member  
**Handler:** `GetOrgDashboardQueryHandler` in `PetService.Application/Queries/`  
**Query store method:** `IOrgDashboardQueryStore.GetDashboardAsync(Guid orgId)`

Runs four EF Core aggregates in parallel (`Task.WhenAll`):

1. Pet counts grouped by status (`Available`, `Reserved`, `Adopted`) for pets where `OrganizationId == orgId`
2. Adoption request counts grouped by status for requests where `OrganizationId == orgId`
3. Total impressions sum from `PetInteractions` joined to org pets
4. Average swipe rate: `SUM(swipes) / NULLIF(SUM(impressions), 0) * 100` across org pets

**Response DTO** (`GetOrgDashboardResponse`):
```csharp
public record GetOrgDashboardResponse(
    int TotalPets,
    int AvailablePets,
    int ReservedPets,
    int AdoptedPets,
    int TotalAdoptionRequests,
    int PendingRequests,
    double AdoptionRate,        // AdoptedPets / TotalPets * 100, 0 if no pets
    long TotalImpressions,
    double AvgSwipeRate         // 0–100 percentage
);
```

### GetOrgDashboardTrendsQuery

**Endpoint:** `GET /api/organizations/{id}/dashboard/trends?from=&to=`  
**Auth:** Authenticated + org member  
**Handler:** `GetOrgDashboardTrendsQueryHandler`  
**Query store method:** `IOrgDashboardQueryStore.GetTrendsAsync(Guid orgId, DateTime from, DateTime to)`

**Validation (in handler):**
- `from` defaults to `DateTime.UtcNow.AddDays(-84)` (12 weeks) if not supplied
- `to` defaults to `DateTime.UtcNow` if not supplied
- `from` must be before `to`; return 400 if not
- Range clamped to max 52 weeks (`364 days`); if exceeded, `from` is moved up

**Queries (EF Core LINQ, no raw SQL):**

Adoptions by week — group `Pets` where `OrganizationId == orgId && Status == Adopted` by `EF.Functions.DateDiffDay(from, adoptedAt) / 7`:
```csharp
_db.Pets
   .Where(p => p.OrganizationId == orgId
            && p.Status == PetStatus.Adopted
            && p.UpdatedAt >= from && p.UpdatedAt <= to)
   .GroupBy(p => EF.Functions.DateDiffDay(from, p.UpdatedAt) / 7)
   .Select(g => new { WeekOffset = g.Key, Count = g.Count() })
```

Requests by week — same grouping on `AdoptionRequests.CreatedAt` for requests linked to org pets.

Both series are projected to a list of `TrendPoint(DateTime WeekStart, string Label, int Count)`. Weeks with zero activity are filled in on the application side (not in SQL) so the chart always has a continuous X-axis.

**Response DTO:**
```csharp
public record GetOrgDashboardTrendsResponse(
    IReadOnlyList<TrendPoint> AdoptionsByWeek,
    IReadOnlyList<TrendPoint> RequestsByWeek
);

public record TrendPoint(DateTime WeekStart, string Label, int Count);
```

### New files (PetService)

| File | Purpose |
|------|---------|
| `Application/Queries/GetOrgDashboardQuery.cs` | Query record + handler |
| `Application/Queries/GetOrgDashboardTrendsQuery.cs` | Query record + handler |
| `Application/Queries/IOrgDashboardQueryStore.cs` | Read interface |
| `Infrastructure/Persistence/OrgDashboardQueryStore.cs` | EF Core implementation |
| `API/Controllers/OrgDashboardController.cs` | Two GET actions |

`IOrgDashboardQueryStore` is registered in `ServiceCollectionExtensions` alongside other query stores.

---

## Frontend

### Page: `OrgDashboard.razor`

**Route:** `/org/{OrgId:guid}/dashboard`  
**Layout:** `MainLayout`  
**Auth:** `[Authorize]`  
**File:** `Pages/Organization/OrgDashboard.razor`

**Load sequence:**
1. On init: fire KPI call and recent-requests call in parallel (`Task.WhenAll`)
2. Trends loaded separately after KPIs render (non-blocking) with default 12-week range
3. Date range change re-fetches only trends

**Layout (MudBlazor):**

```
MudGrid
  ├── 4 × MudItem xs=12 sm=6 md=3   → KPI cards (MudPaper)
  ├── MudItem xs=12                  → Date range pickers + Apply button
  ├── 2 × MudItem xs=12 sm=6        → MudChart (ChartType.Bar) — Adoptions | Requests
  └── MudItem xs=12                  → Recent requests table + "View all" link
```

**KPI cards** — each `MudPaper` contains:
- `MudIcon` (large, colored)
- Primary number (`Typo.h4`)
- Label (`Typo.body2`, secondary color)
- Sub-detail line (e.g. chips for Available/Reserved/Adopted under Total Pets)

**Bar charts** — `MudChart ChartType="ChartType.Bar"` with:
- `ChartSeries` bound to `TrendPoint.Count` values
- `XAxisLabels` bound to `TrendPoint.Label` values
- `Height="300"`, `Width="100%"`
- Loading skeleton (`MudSkeleton`) while trends fetch

**Recent requests table** — `MudTable` (not `MudDataGrid`, no server paging needed for 10 rows):
- Columns: Pet name · Status chip · Message preview (60-char truncate + ellipsis) · Submitted (relative, e.g. "3 days ago") · Actions (Approve/Reject for Pending)
- "View all →" `MudLink` top-right navigating to `/organization/{OrgId}/adoption-requests`
- Approve/Reject call existing `PetApiClient` methods; reload the 10-row list on success

**API client additions** (`PetApiClient.cs`):
```csharp
Task<OrgDashboardResponse?> GetOrgDashboardAsync(Guid orgId);
Task<OrgDashboardTrendsResponse?> GetOrgDashboardTrendsAsync(Guid orgId, DateTime? from, DateTime? to);
```

**New model records** (`Models/ApiModels.cs`):
```csharp
public record OrgDashboardResponse(int TotalPets, int AvailablePets, int ReservedPets,
    int AdoptedPets, int TotalAdoptionRequests, int PendingRequests,
    double AdoptionRate, long TotalImpressions, double AvgSwipeRate);
public record OrgDashboardTrendsResponse(
    IReadOnlyList<TrendPoint> AdoptionsByWeek,
    IReadOnlyList<TrendPoint> RequestsByWeek);
public record TrendPoint(DateTime WeekStart, string Label, int Count);
```

### Navigation update

In `MainLayout.razor` (or wherever org nav links are rendered), prepend a **Dashboard** nav item pointing to `/org/{orgId}/dashboard`. The `orgId` is already available from the JWT claim or NavigationManager.

---

## Error handling

| Scenario | Behaviour |
|----------|-----------|
| KPI fetch fails | `MudAlert Severity.Error` in place of cards; other sections still load |
| Trends fetch fails | `MudAlert` inside chart area; KPIs and requests unaffected |
| Requests fetch fails | `MudAlert` inside table area |
| `from > to` on trends | Handler returns 400; Blazor shows snackbar |
| No data (new org) | Charts render with all-zero series; cards show `0`; table shows empty-state text |

---

## Out of scope

- No push/real-time updates (polling not needed for a dashboard)
- No export (CSV/PDF)
- No per-pet drill-down from the dashboard (OrgMetrics page handles that)
- No changes to existing pages

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `GetOrgDashboardQueryHandlerTests` — mock query store, verify response mapping
- `GetOrgDashboardTrendsQueryHandlerTests` — verify date defaulting, clamping, zero-fill logic

**Integration tests** (`PetService.IntegrationTests`):
- `OrgDashboardControllerTests` — seed org with pets in various statuses + interactions, assert KPI values; assert trends group correctly by week

No Blazor tests (no FE test infrastructure in this repo).
