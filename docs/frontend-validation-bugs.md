# Frontend Validation & Input Bugs

Audit of every form / input in the Blazor WASM app (`src/Web/PetAdoption.Web.BlazorApp/`).
Each finding lists the file, the symptom, and the backend rule it should mirror.

---

## Critical (broken behavior, data loss, or unusable UI)

### 1. Pet Type dropdown is empty when creating an Org pet
`Pages/Organization/ManageOrgPets.razor:91`

```csharp
var dialog = await DialogService.ShowAsync<PetFormDialog>("Add Pet");
```

`PetTypes` parameter is never passed, so the dropdown in `PetFormDialog` is always empty. The page never even loads pet types (no `_petTypes` field, no call to `GetPetTypesAsync`). Submit is blocked because Pet Type is required → user can never create an org pet.

**Fix:** load pet types in `OnInitializedAsync` (mirror `ManagePets.razor:66-74`) and pass them via `DialogParameters` for both create and edit.

---

### 2. Admin pet form silently drops Tags
`Pages/Admin/ManagePets.razor:106, 122-131, 139`

`PetFormDialog` fully supports tags, but `ManagePets`:
- never seeds existing tags into the edit dialog (no `{ x => x.Tags, pet.Tags?.ToList() }` parameter)
- never sends `data.Tags` to the API on create or update

So opening "Edit Pet" wipes whatever tags were on the pet, and "Add Pet" drops tags the admin typed before submit. Both `CreatePetRequest` and `UpdatePetRequest` already have `Tags` properties (`Models/ApiModels.cs:16-17`), so this is a pure wiring bug.

---

### 3. Confirm-password validation goes stale
`Pages/Public/Register.razor:25, 51-52`

```csharp
private string? ValidateConfirmPassword(string confirmPassword) =>
    confirmPassword != _password ? "Passwords do not match" : null;
```

`Validation` runs only when the confirm field changes. If the user types the password *after* the confirm field, the mismatch error never appears (and vice versa) until they touch the confirm field again. Either trigger `_form.Validate()` from the password field's `OnDebounceIntervalElapsed`/`@bind-Value:after`, or move the check into `HandleRegister` as a pre-flight.

---

### 4. Announcement dates: no `End > Start` check on the client
`Components/Shared/AnnouncementFormDialog.razor:9-10`

Backend throws `InvalidAnnouncementDates` (`Domain/Announcement.cs:43-46`) when `EndDate <= StartDate`. The form has no validator, so the user gets a generic snackbar after the round-trip. Add a `Validation` func on the End picker comparing against `_startDate`.

---

## Validation gaps (server rejects what UI accepts)

### 5. Pet Name — no MaxLength
`Components/Shared/PetFormDialog.razor:10`

Server rule: `1..100` chars (`PetName.cs:10-11`). UI has `Required` only. `MudTextField MaxLength="100"` plus a counter would prevent the round-trip.

### 6. Pet Breed — no MaxLength
`Components/Shared/PetFormDialog.razor:20`

Server rule: `1..100` chars (`PetBreed.cs:15-16`). UI has no length cap. Note also: `PetBreed` *cannot be empty* if you set it, but the form treats it as optional — typing then deleting leaves an empty string that the API may or may not coerce to null. Confirm whether the API client maps `""` → `null`.

### 7. Pet Description — no MaxLength, no counter
`Components/Shared/PetFormDialog.razor:22`

Server rule: `1..2000` chars (`PetDescription.cs:15-16`). Long descriptions silently 400.

### 8. Pet Age — no upper bound
`Components/Shared/PetFormDialog.razor:21` and discover filters in `Pages/User/Discover.razor:30-33`

`PetAge` only validates `>= 0`, but pets don't live to 100,000 months. UI should set a reasonable `Max` (e.g. 360 months / 30 years) on every age numeric field to prevent typos.

### 9. Pet Tag — no MaxLength
`Components/Shared/PetFormDialog.razor:26`

Server rule: `1..50` chars, lowercased (`PetTag.cs:10-11`). UI lowercases on add but lets the user enter tags >50 chars, which the API rejects. Add `MaxLength="50"` and a helper text.

### 10. Announcement Title / Body — no MaxLength
`Components/Shared/AnnouncementFormDialog.razor:7-8`

Server rules: title `1..200`, body `1..5000`. Add `MaxLength` and counters.

### 11. Pet Type Code — no character-set restriction
`Components/Shared/PetTypeFormDialog.razor:7`

Backend lowercases the code (`PetType.Create`) and requires uniqueness, but the UI accepts spaces, punctuation, mixed case. The user has no idea what they typed will be stored as `"some code!"` and conflict with `"some code!"`. Add a regex validator (e.g. `^[a-z0-9_-]+$`) and show the normalized form in helper text.

### 12. Register / Login Email — no real validation
`Pages/Public/Register.razor:17-18`, `Pages/Public/Login.razor:15-16`

`InputType.Email` only enables the browser-level mobile keyboard; no MudBlazor `Validation` runs. Server requires `@` and `.` (`Email.cs:17-18`). Add a simple regex validator and a friendly error string.

### 13. Register / Profile Phone — no validation
`Pages/Public/Register.razor:19`, `Pages/User/Profile.razor:25`

Server rule: 10–15 digits when present (`PhoneNumber.cs:19-20`). UI accepts anything, including text. Add a digit-only mask or pattern validator.

### 14. Register / ChangePassword — "Minimum 8" hint without enforcement
`Pages/Public/Register.razor:22`, `Pages/User/Profile.razor:42`

Server rule: 8–100 chars (`Password.ValidatePlainText`). The UI displays the hint but `Required` is the only gate, so a 1-char password reaches the API and bounces back as a generic error.

### 15. Profile Full Name — no MinLength=2
`Pages/User/Profile.razor:23`

Server rule: 2–100 chars (`FullName.cs:16-20`). `Required` allows a single character. Add `MinLength`/`MaxLength` and a custom validator.

### 16. Adoption message / Reject reason — no MaxLength
`Pages/User/Favorites.razor:120`, `Pages/Organization/OrgAdoptionRequests.razor:64`

I couldn't find an explicit server cap, but at minimum these should have a sane UI cap (e.g. 1000 chars) and a counter to avoid pasted essays. Reject reason additionally relies only on the disabled-button check (`string.IsNullOrWhiteSpace`); whitespace-only is blocked but the field isn't inside a `MudForm`, so `Required` semantics are inconsistent with the rest of the app.

---

## Logic bugs in filter / range inputs

### 17. Discover filters: Min Age can exceed Max Age
`Pages/User/Discover.razor:30-33`

Two independent numeric fields, no cross-validation. The query goes through, returns 0 pets, user sees "You've seen all available pets" — misleading. Add a validator that disables Apply when `_minAge > _maxAge`.

### 18. Org Metrics date range: To can precede From
`Pages/Org/OrgMetrics.razor:14-21`

Same problem; no validation. The Apply button always runs.

---

## Minor / UX

### 19. `PetFormDialog` Pet Type dropdown shows no loading state
`Components/Shared/PetFormDialog.razor:11-19`

If `PetTypes` is still loading, the select is enabled with zero options. Show a `MudProgressCircular` adornment or disable the select until items arrive.

### 20. Generic catch swallows all errors in profile/discover/etc.
e.g. `Pages/User/Profile.razor:91, 109, 131`

Every `catch { Snackbar.Add("Connection error", ...); }` hides 4xx validation responses behind a connection-error message. The non-ok branch reads `ApiError` correctly; only the truly thrown branch should say "connection error". This makes server-side validation failures feel like network outages.

### 21. `AnnouncementFormDialog` parameter defaults vs OnInitialized
`Components/Shared/AnnouncementFormDialog.razor:26-37`

`StartDate` / `EndDate` are `DateTime` (non-nullable) parameters with hard-coded defaults `DateTime.UtcNow` / `+7d`. On the *create* path the consumer passes nothing, but the parameters are already initialized to those defaults — works, but it means the create dialog opens with `Now` and `Now + 7d` evaluated at component-instance creation, which can be a few hours stale if the WASM session is long-lived. Use `DateTime?` parameters and set defaults inside `OnInitialized` if either is null.

### 22. Reject dialog vs other dialogs — inconsistent pattern
`Pages/Organization/OrgAdoptionRequests.razor:59-71` uses an inline `<MudDialog @bind-Visible>`; the rest of the app uses `IDialogService.ShowAsync<...>`. Not a bug, but the inline form skips `MudForm`, so `Required` on the textarea isn't actually enforced; only the disabled-button gate is.

---

## Quick wins (low effort, high value)

1. Wire `PetTypes` into `ManageOrgPets.razor` (#1) — the page is currently broken.
2. Pass `Tags` through admin Pet create/edit (#2) — silent data loss.
3. Add `MaxLength` to every text input that maps to a value object (#5–#11).
4. Add password-length and email-format validators on Register / Profile (#12, #14).
5. Add cross-field validators on Min/Max age and Start/End date (#4, #17, #18).
