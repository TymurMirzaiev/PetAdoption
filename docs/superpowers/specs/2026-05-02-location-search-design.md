# Location-Based Pet Search — Design Spec

**Date:** 2026-05-02
**Status:** Draft
**Scope:** PetService backend (Organization domain extension + Discover query filter + new Address endpoint) + Blazor WASM frontend (Discover location chip + slider + geolocation interop)

---

## Overview

Adds a physical location to each Organization (lat/lng + city/region label) and lets users filter Discover to "pets within X km of me". Pets inherit their organization's location via `Pet.OrganizationId → Organization.Address`. The frontend reads the user's location through the browser Geolocation API (with permission), with manual city/postcode entry as a fallback. The Discover query gains optional `lat`, `lng`, and `radiusKm` parameters; the existing infinite-swipe flow is unchanged when no location is supplied.

---

## Architecture

Read-path filter only — no new domain events, no outbox traffic. The Discover handler in PetService gains an optional location predicate that is pushed into the query store, which joins `Pets` to `Organizations` and filters by Haversine distance computed in SQL.

```
PetsController GET /api/pets/discover?lat=&lng=&radiusKm=
  → GetDiscoverPetsQuery (extended)
  → IPetQueryStore.GetDiscoverable (extended: optional lat/lng/radiusKm)

OrganizationsController POST /api/organizations/{orgId}/address
  → SetOrganizationAddressCommand
  → IOrganizationRepository
```

---

## Domain changes

### `Address` value object on `Organization`

New value object in `PetAdoption.PetService.Domain.ValueObjects`:

```csharp
public record Address(
    decimal Lat,            // -90..90
    decimal Lng,            // -180..180
    string Line1,
    string City,
    string Region,
    string Country,
    string PostalCode);
```

- Validation in constructor: lat/lng range, all string fields trimmed and non-empty (Line1 + City + Country required; Region and PostalCode optional, can be empty string).
- Throws `DomainException` with a new `invalid_organization_address` error code (mapped to 400 in `ExceptionHandlingMiddleware`).
- `Organization.SetAddress(Address address)` method replaces any existing address. Raises `OrganizationAddressUpdatedEvent` (informational, no downstream consumers in v1).

### Storage choice

Store **lat/lng as `decimal(9,6)`** columns and compute Haversine in SQL — not the SQL Server `geography` type. Reasons:
- Simpler EF Core mapping (`HasConversion` for the `Address` value object as owned type, scalar `decimal` columns).
- Portable across the test Testcontainers setup.
- Haversine in T-SQL is one expression and well-indexed by a coarse bounding-box pre-filter on lat/lng.

Index: composite `(Lat, Lng)` on the `Organizations` table for the bounding-box pre-filter.

---

## Backend

### Endpoint: `GET /api/pets/discover` (extended)

**New query params:** `lat` (decimal), `lng` (decimal), `radiusKm` (int, 1–500).
**Validation in handler:**
- All three must be supplied together, or none. If partial, return `400 invalid_location_filter`.
- `radiusKm` clamped to `[1, 500]`.
- Lat/lng range-checked.

**Query store change (`IPetQueryStore.GetDiscoverable`):** add optional `(decimal? lat, decimal? lng, int? radiusKm)` parameters. When supplied, join `Pets` to `Organizations` (inner join — pets without an org or whose org has no address are excluded from location-filtered results) and apply:

1. **Bounding-box pre-filter** on `Organization.Lat` and `Organization.Lng` (cheap, uses index): roughly `±radiusKm/111` degrees lat, `±radiusKm/(111·cos(lat))` degrees lng.
2. **Haversine fine-filter** in `Where(...)`: `2 * 6371 * ASIN(SQRT(...))` expression in EF Core LINQ using `Math.Sin/Cos/Asin/Sqrt`. EF Core translates these to T-SQL `SIN/COS/ASIN/SQRT`.
3. Result distance is **not** projected back to the DTO in v1 — only used for filtering. (Sorting remains the existing random/feed order.)

When location params are absent, the query path is unchanged.

### Endpoint: `POST /api/organizations/{orgId}/address`

**Auth:** Authenticated + org member with `Admin` role for that org (existing `OrgAuthorizationFilter`).
**Body:** `SetOrganizationAddressRequest(decimal Lat, decimal Lng, string Line1, string City, string Region, string Country, string PostalCode)`.
**Handler:** `SetOrganizationAddressCommandHandler` calls `org.SetAddress(...)` and saves via `IOrganizationRepository`.
**Response:** `200 OK` with the updated `OrganizationDto` including the new address fields.

### New / modified files (PetService)

| File | Change |
|------|--------|
| `Domain/ValueObjects/Address.cs` | New value object |
| `Domain/Organization.cs` | Add `Address?`, `SetAddress(...)` |
| `Infrastructure/Persistence/OrganizationEntityConfiguration.cs` | Map `Address` as owned type |
| `Application/Commands/SetOrganizationAddressCommand.cs` | New command + handler |
| `Application/Queries/GetDiscoverPetsQuery.cs` | Add `Lat?`, `Lng?`, `RadiusKm?` params |
| `Application/Abstractions/IPetQueryStore.cs` | Extend `GetDiscoverable` signature |
| `Infrastructure/Persistence/PetQueryStore.cs` | Bounding-box + Haversine filter |
| `API/Controllers/OrganizationsController.cs` | New POST `/{orgId}/address` action |

---

## Frontend

### Discover page (`Pages/User/Discover.razor`)

Add a **location chip + slider** above the existing filter panel:

- Chip shows current state: `📍 Within 50 km of Berlin` (when active) or `📍 Set location` (when inactive).
- Click chip opens a `MudDialog` with two tabs:
  1. **Use my location** — calls JS interop `geolocation.getMyPosition()`, displays a snackbar on permission denial and falls back to tab 2.
  2. **Enter manually** — `MudTextField` for City/Postcode + Country dropdown. Resolved client-side via a free reverse-geocode call **out of scope for v1** — instead, manual entry just stores the typed string + uses 0 lat/lng (filter is skipped if no coords). v1 manual entry effectively only labels the chip; coordinate-based filtering requires geolocation permission.
- **Radius `MudSlider`** (0–500 km, default 50). `0` means "no location filter" (chip cleared).
- Selected lat/lng/radius persisted to `localStorage` (`petadoption_discover_location`) so the user doesn't re-grant permission every visit.

The existing `FetchBatch` call in `Discover.razor` adds `lat`, `lng`, `radiusKm` parameters to `PetApi.GetDiscoverPetsAsync(...)` when location is active.

### JS interop helper (`wwwroot/js/geolocation.js`)

```js
export function getMyPosition() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) return reject('unsupported');
    navigator.geolocation.getCurrentPosition(
      pos => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
      err => reject(err.message),
      { timeout: 10000, maximumAge: 600000 });
  });
}
```

Called via `IJSRuntime.InvokeAsync<GeoPosition>("getMyPosition")` from the dialog. Errors handled with a `MudSnackbar` ("Location permission denied — enter a city instead").

### Org settings page (out of this spec's UI scope but enabled by it)

The `POST /api/organizations/{orgId}/address` endpoint is consumed by the existing org settings page (a small follow-up — not part of this spec) which already has a free-form profile section. Adding the address form there is straightforward and not detailed here.

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `AddressTests` — constructor validation: lat/lng range, required strings, trimming.
- `OrganizationTests` — `SetAddress` replaces existing, raises event.
- `GetDiscoverPetsQueryHandlerTests` — partial-location params return 400; `radiusKm` clamping; passing through to query store.

**Integration tests** (`PetService.IntegrationTests`):
- `OrganizationsControllerTests` — POST address: 401/403 for non-admin, 200 + persisted for org admin, 400 for invalid lat.
- `DiscoverLocationFilterTests` — seed three orgs at known coordinates (Berlin, Munich, NYC), pets in each; assert `?lat=&lng=&radiusKm=100` from Berlin returns Berlin pets only, `radiusKm=600` includes Munich, NYC always excluded.
- Verify Haversine SQL translation succeeds on Testcontainers SQL Server (no client-eval fallback).

---

## Out of scope

- **Map view** in Discover (Leaflet / Google Maps embed) — Phase 2.
- **Route directions** to the shelter from the pet detail page.
- **Real-time location tracking** of the user.
- **Reverse geocoding** of manual city/postcode entry — v1 manual entry is label-only; filtering requires browser geolocation.
- **Sorting by distance** — v1 only filters; ordering remains the existing feed order.
- **Distance in DTO** — pet cards do not show "12 km away" in v1.
- **Per-pet location override** (a pet at a different address than its org).
