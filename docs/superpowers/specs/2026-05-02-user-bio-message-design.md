# User Bio for Adoption Requests — Design Spec

**Date:** 2026-05-02
**Status:** Approved
**Scope:** UserService domain + API (new `Bio` value object on User), PetService API (Message becomes optional, falls back to JWT bio claim), Blazor WASM frontend (Profile page + adoption dialog)

---

## Overview

Today every adoption request forces the adopter to type a fresh message in a popup before submitting. That popup is friction in front of the primary conversion event. Instead, users write a one-time **Bio** on their Profile page; when they request adoption, the bio is auto-attached as the request `Message` and the popup collapses to a one-click flow (with an inline edit affordance for users who still want to customise per request). The change removes a typing step from the hottest path in the app while keeping shelters' existing per-request `Message` view intact.

---

## Domain changes

### UserService — new `Bio` value object on `User`

- New value object `Bio` (UserService.Domain.ValueObjects):
  - Stored as `string?` (nullable — bio is optional)
  - Validation in constructor: trim, max 1000 chars, throw `ArgumentException` if longer. Empty/whitespace input becomes `null` via `Bio.FromOptional(string?)`.
- `User` aggregate gets `public Bio? Bio { get; private set; }` plus mapping in `UserEntityConfiguration` via `HasConversion` (UserId/Email pattern), column `Bio nvarchar(1000) NULL`.
- `User.UpdateProfile()` extended with optional `string? bio = null` parameter. Following the existing "always update phone" idiom, treat `bio` the same way: always write `Bio.FromOptional(bio)` so callers can clear it by passing `null`/empty.
- `UserProfileUpdatedEvent` extended with `string? Bio` field — outbox consumers ignore it for now, but downstream services (PetService) can subscribe later if we ever decide to cache bio server-side.

### PetService — no schema change

`AdoptionRequest.Message` stays exactly as is (`string?`, persisted, trimmed in `Create`). The bio is **copied into `Message` at request creation time** — it is a snapshot, not a live join. This preserves shelter-side history (each request reflects the bio the user had when they submitted it) and keeps PetService ignorant of the User aggregate.

---

## Cross-service: how PetService gets the bio

**Decision: include `bio` as a JWT claim issued by UserService at login/refresh.**

- UserService adds `bio` claim to the access token alongside the existing `sub`, `email`, `role`, `userId` claims (`TokenService` / `JwtTokenGenerator`).
- Empty/null bio → claim is omitted (do not emit empty string claim).
- PetService reads `User.FindFirst("bio")?.Value` in the `CreateAdoptionRequest` controller action (already JWT-authenticated).
- Staleness: bio rarely changes; the existing 30-day refresh-token rotation refreshes claims on next refresh. Acceptable lag: at most one access-token lifetime (~15 min). We do **not** add a synchronous PetService→UserService HTTP call — that adds latency to the most critical user action and a runtime dependency between services.

---

## API changes

### UserService — `PUT /api/users/me`

Request body adds optional `Bio`:

```csharp
public record UpdateUserProfileRequest(string? FullName, string? PhoneNumber, string? Bio);
```

Handler passes `Bio` through to `User.UpdateProfile(..., bio: request.Bio)`. `GET /api/users/me` response (`UserProfileResponse`) gains `string? Bio` so the Profile page can rehydrate the field. Validation errors (>1000 chars) bubble up as `ArgumentException` → 400 via existing middleware.

### PetService — `POST /api/adoption-requests`

`Message` becomes optional:

```csharp
public record CreateAdoptionRequestRequest(Guid PetId, string? Message);
```

Handler logic:

1. If `request.Message` is non-null and non-empty → use it (per-request override path).
2. Else → read `bio` claim from the authenticated principal; use it if present.
3. Else → pass `null` to `AdoptionRequest.Create`. The aggregate already accepts null/empty messages — shelter-side UI just shows "(no message)".

The handler does **not** care whether the message came from override or bio. Empty strings (`""`) sent by the client are treated as "no override" and fall through to bio (avoids the dialog sending blank-string overrides that override the bio with nothing).

---

## Frontend changes (Blazor WASM)

### `Pages/User/Profile.razor`

Add a third `MudPaper` block titled **"About me"** between Personal Information and Change Password:

- `MudTextField` `Lines="4"` `MaxLength="1000"` `Counter="1000"` bound to `_bio`.
- Helper text: `"Shown to shelters when you request adoption. You can still write a custom message per pet."`
- Save uses the existing `UpdateProfileAsync` call — we just include `Bio = _bio` in the payload object (`new { FullName, PhoneNumber, Bio }`).
- Initial value loaded from `_profile.Bio` in `OnInitializedAsync`.

### `Pages/User/Favorites.razor` and Pet detail "Adopt" flow

The existing `MudDialog` is **replaced with a slim confirmation dialog** that pre-fills the message from the user's bio:

- `OpenAdoptionDialog` reads the cached profile bio (already available via `JwtAuthenticationStateProvider`'s decoded claims, or from a one-shot `UserApi.GetMyProfileAsync()` cached on the page).
- Dialog body becomes a single line: `"Send your bio to {OrgName}?"` with the bio shown in a read-only `MudPaper` preview, plus a small `"Edit message for this pet"` `MudLink` that toggles an inline `MudTextField` (same field as today, prefilled with the bio).
- Primary button text: `"Send Request"` (one click). Secondary: `"Cancel"`.
- If the user has no bio: dialog shows the existing free-text editor with a one-line hint `"Tip: add a bio in your profile to skip this next time"` linking to `/profile`.

The existing `PetApiClient.CreateAdoptionRequestAsync(petId, message)` signature does not change — when the user has not edited the message, we still pass the bio as `message` (simpler than teaching the client about claims). The server-side fallback exists for non-Blazor clients.

---

## Migration / backwards compat

- Existing `User` rows: `Bio` column is nullable, no backfill needed.
- Existing `AdoptionRequest.Message` rows are untouched.
- Existing JWTs without a `bio` claim continue to work; PetService treats missing claim as empty bio.
- Old Blazor clients that always send a `Message` continue to work — server-side override path takes precedence over the claim.

---

## Testing

**UserService unit tests:**
- `BioTests` — empty/whitespace becomes null via `FromOptional`, >1000 chars throws, exactly 1000 chars allowed, trims surrounding whitespace.
- `UserTests.UpdateProfile_WithBio_*` — sets bio, clears bio (null), domain event payload includes bio.

**UserService integration tests:**
- `PUT /api/users/me` with `Bio` persists and round-trips on `GET /api/users/me`.
- Login response JWT contains `bio` claim when set, omits it when null.

**PetService integration tests:**
- `POST /api/adoption-requests` with no body `Message` and `bio` claim present → request stored with `Message == bio`.
- With body `Message` present → body wins (override path).
- With neither → `Message` is null, request still created.

**Blazor:** manual smoke — bio prefilled in dialog, "Edit message" toggle works, profile save round-trips. No new Blazor unit tests (none exist for these pages today).

---

## Out of scope

- Per-request templates / saved snippets
- Bio versioning or edit history
- Profile photo / richer profile (separate spec)
- Per-user "always skip the dialog" preference flag — v1 keeps the one-click dialog as a confirmation step
- Real-time bio sync to PetService (if it ever matters, subscribe to `UserProfileUpdatedEvent` on the existing `user.events` exchange)
