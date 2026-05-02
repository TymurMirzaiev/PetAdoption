# Blazor Frontend Code Review

## Dead Code

- `src/Web/PetAdoption.Web.BlazorApp/wwwroot/css/app.css:149` — `.onboarding-step-enter` CSS class is dead. It has the same `animation` declaration as `.onboarding-step`. The class is toggled on for only 50ms (a `Task.Delay(50)` window), shorter than the animation duration, so it never visually fires.

- `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/PetFormDialog.razor:45–98` — The Media tab and Medical tab are only rendered when `IsEdit && PetId.HasValue && OrgId.HasValue`. Neither `ManagePets.razor` nor `ManageOrgPets.razor` ever passes `PetId` or `OrgId` when opening the edit dialog, so both tabs are permanently unreachable from existing callers.

- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Adoptions.razor` — Calls `GetPetsAsync(status: "Adopted")` globally with no user filter. This is an admin-scoped query; the page likely returns pets adopted by any user (functionally wrong or empty for most users).

- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Reservations.razor` — Same issue: calls `GetPetsAsync(status: "Reserved")` globally with no user filter.

## Duplicated Code

- **`StatusColor` (pet status) — copied into 5 files**: `Pages/Admin/ManagePets.razor`, `Pages/Organization/ManageOrgPets.razor`, `Pages/User/Favorites.razor`, `Pages/User/PetDetail.razor`, `Components/Shared/PetCard.razor`.

- **`StatusColor` (adoption request status) — copied into 3 files**: `Pages/Organization/OrgAdoptionRequests.razor:151`, `Pages/Organization/OrgDashboard.razor:406`, `Pages/User/MyAdoptionRequests.razor:93`.

- **`FormatAge` — copied into 6 files**: `Pages/Admin/ManagePets.razor:214`, `Pages/Organization/ManageOrgPets.razor:187`, `Pages/User/Favorites.razor:294`, `Pages/User/PetDetail.razor:225`, `Pages/User/Adoptions.razor:71`, `Components/Shared/PetCard.razor:40`.

- **Inline reject dialog — duplicated in two pages**: the entire `<MudDialog @bind-Visible="_rejectDialogVisible">` block with private fields `_rejectRequestId`, `_rejectPetName`, `_rejectReason`, and methods `OpenRejectDialog`/`SubmitReject` is copy-pasted between `Pages/Organization/OrgAdoptionRequests.razor:63–149` and `Pages/Organization/OrgDashboard.razor:216–390`.

- **Pet list card layout — near-identical in `Reservations.razor` and `Adoptions.razor`**: both render `MudGrid` → `MudItem` → `MudCard` with avatar initial, name, type, breed, age.

- **Query string building pattern — repeated 6× in `PetApiClient.cs`**: `GetPetsAsync`, `GetDiscoverPetsAsync`, `GetFavoritesAsync`, `GetOrgPetsAsync`, `GetOrgMetricsAsync`, `GetOrgDashboardTrendsAsync` all use manual `var query = "base?x=y"; if (...) query += "&z=..."` string building. Only `breed` is `Uri.EscapeDataString`-escaped; `tags` in `GetOrgPetsAsync` is not.

## Bad Practices

- **`Pages/User/Discover.razor:138–174`** — Location persistence (serializing `lat,lng,radius` into `localStorage`) is implemented directly in the page `@code` block. I/O logic belongs in a service.

- **`Pages/User/Discover.razor:339–383`** — Onboarding key management (`petadoption_onboarding_seen` in `localStorage`) is in the page. Should be a service.

- **`Components/Layout/MainLayout.razor:53–78`** — A `System.Threading.Timer` runs directly inside the layout component to poll unread chat counts. Timer lifecycle, API calls, and state all sit in the layout. Should be a scoped `ChatUnreadService`.

- **`Pages/Organization/OrgDashboard.razor:326–337`** — Domain error detection by `ex.Message.Contains("End date must be after start date")` string matching. Fragile; the API should return a structured `ApiError` that the client reads with `ReadFromJsonAsync<ApiError>`.

- **`object`-typed request parameters in API clients lose type safety**: `Services/UserApiClient.cs:33` (`UpdateProfileAsync(object request)`), `Services/UserApiClient.cs:37` (`ChangePasswordAsync(object request)`), `Services/PetApiClient.cs:53,56,122,125` (four methods). Callers pass anonymous objects with no compile-time check on field names or types.

- **Silent `catch { }` blocks swallow failures**: `Pages/Admin/ManagePets.razor:73`, `Pages/Organization/ManageOrgPets.razor:79`, `Pages/User/Favorites.razor:167`, `Components/Shared/AnnouncementBanner.razor:19` — failures are swallowed with no user feedback. Additionally, `PetApiClient.cs:257–273` catches internally, then `MainLayout.razor:75,78` wraps those in another `catch { }` — double-silent failure.

- **Unsafe `[..1]` string slice on potentially empty `Pet.Name`**: `Components/Shared/PetCard.razor:6`, `Pages/User/Favorites.razor:66`, `Pages/User/Adoptions.razor:31`, `Pages/User/Reservations.razor:32`, `Pages/User/PetDetail.razor:23` — all throw `ArgumentOutOfRangeException` if `Name` is empty. Should guard with `pet.Name.Length > 0 ? pet.Name[..1] : "?"`.

- **Inline dialogs bypass `IDialogService` inconsistently**: `Pages/Organization/OrgDashboard.razor:216–228`, `Pages/Organization/OrgAdoptionRequests.razor:63–75`, `Pages/User/Favorites.razor:113–140` use `@bind-Visible` inline dialogs. The rest of the codebase uses `IDialogService.ShowAsync<T>`.

- **`SwipeCardStack` mutates a `[Parameter]` directly**: `Components/Shared/SwipeCardStack.razor:156` — `Pets.RemoveAt(0)` mutates the list passed in as a `[Parameter]`. Parent owns parameter values; this bypasses the component lifecycle.

- **`ChatPanel` passes JWT in URL query string**: `Components/Chat/ChatPanel.razor:93–95` — `$"{petApiBase}/hubs/chat?access_token={token}"` exposes the JWT in the URL (server logs, browser history). SignalR's `WithUrl` `AccessTokenProvider` option should be used instead.

- **`AnnouncementFormDialog` mutates `[Parameter]` properties via two-way binding**: `Components/Shared/AnnouncementFormDialog.razor:7–8` — `@bind-Value="Title"` and `@bind-Value="Body"` bind directly to `[Parameter]` properties. Should copy to private backing fields in `OnInitialized`.

- **`Profile.razor` has two "Save Changes" buttons calling the same method**: `Pages/User/Profile.razor:29–47` — "Personal Information" and "About Me" sections each have a button that calls `UpdateProfile()`, which sends all fields in one request. Validation state `_profileValid` is also shared.

- **Hard-coded pixel dimensions and raw colors bypass MudBlazor theme**: `Components/Shared/PetMediaGallery.razor:27` — `border: 2px solid gold`; `Components/Shared/OnboardingDialog.razor:31,44,57` — hard-coded RGBA background/border colors.

## Refactoring Opportunities

- **Extract `PetDisplayHelpers` static class** with `PetStatusColor(string)`, `RequestStatusColor(string)`, `FormatAge(int)` — eliminates 14+ copies across the codebase.

- **Extract `RejectRequestDialog` as a proper dialog component** (`Components/Shared/RejectRequestDialog.razor`) using `IDialogService`. Takes `PetName` as a parameter and returns the reason via `DialogResult`. Eliminates ~60 duplicated lines between `OrgAdoptionRequests.razor` and `OrgDashboard.razor`.

- **Extract `PetListCard` component** for the near-identical card layout in `Reservations.razor` and `Adoptions.razor`, with a `[Parameter] RenderFragment? Actions` slot.

- **Extract `ILocalStorageService`** — `localStorage` JS interop strings appear in `Discover.razor`, `JwtAuthenticationStateProvider.cs`, and `AuthorizationMessageHandler.cs`. A typed service centralizes the interop and makes components testable.

- **Extract `ChatUnreadService`** — move the `Timer`, API polling, and unread-count state out of `MainLayout.razor` into a scoped `IChatUnreadService`.

- **Use `QueryHelpers.AddQueryString`** (from `Microsoft.AspNetCore.WebUtilities`) in `PetApiClient` to replace 6 instances of manual query string building. This also handles URL encoding correctly for all parameters.

- **Add typed request records to `ApiModels.cs`** to replace the 6 `object`-typed parameters in `PetApiClient` and `UserApiClient`.
