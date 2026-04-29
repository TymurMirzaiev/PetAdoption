
# Blazor UI Design Spec

## Overview

A Blazor WebAssembly single-page application for the PetAdoption platform. Two UI modes: a Tinder-style swipe interface for adopters discovering pets, and a traditional dashboard for shelter administrators. Built with MudBlazor (default dark theme), authenticated via JWT with refresh tokens, calling PetService and UserService APIs directly over HTTP.

## Tech Stack

- Blazor WebAssembly (.NET 9.0)
- MudBlazor component framework (default dark palette)
- JWT authentication with refresh tokens (localStorage)
- Google SSO via Google Identity Services JS library
- HTTP clients to PetService (port 8080) and UserService (port 5000)

## Project Structure

```
src/Web/PetAdoption.Web.BlazorApp/
├── Program.cs                    # Service registration, HttpClients, auth setup
├── wwwroot/
│   └── index.html                # WASM host page, Google Identity Services script tag
├── Auth/
│   ├── JwtAuthenticationStateProvider.cs   # Custom AuthenticationStateProvider (localStorage)
│   └── AuthorizationMessageHandler.cs      # DelegatingHandler: attach Bearer, handle refresh
├── Services/
│   ├── PetApiClient.cs           # Typed HttpClient for PetService
│   └── UserApiClient.cs          # Typed HttpClient for UserService
├── Pages/
│   ├── Public/                   # Login, Register, Landing
│   ├── User/                     # Discover, PetDetail, Favorites, Reservations, Adoptions, Profile
│   └── Admin/                    # Dashboard, ManagePets, PetTypes, Announcements, ManageUsers
├── Components/
│   ├── Layout/                   # MainLayout, AdminLayout, NavMenus
│   └── Shared/                   # SwipeCard, PetCard, ConfirmDialog, GoogleSignInButton, etc.
└── Models/                       # Client-side DTOs (mirroring API responses)
```

Two named `HttpClient` registrations: `"PetApi"` and `"UserApi"`. `AuthorizationMessageHandler` attaches JWT from localStorage to outgoing requests. MudBlazor registered in `Program.cs` with default dark theme.

## Authentication Flow

1. **Login:** User submits credentials -> `UserApiClient` calls `POST /api/users/login` -> receives access token + refresh token.
2. **Storage:** Both tokens saved to `localStorage` via JS interop.
3. **AuthenticationStateProvider:** `JwtAuthenticationStateProvider` reads the token from localStorage, parses claims (userId, roles, expiry), exposes them as a `ClaimsPrincipal`.
4. **Outgoing requests:** `AuthorizationMessageHandler` pulls the token from localStorage and adds `Authorization: Bearer <token>` to every request.
5. **Token refresh:** `AuthorizationMessageHandler` checks token expiry before each request. If expired or near-expiry, calls `POST /api/users/refresh` with the refresh token to get a new pair. If refresh fails (revoked/expired), clears localStorage and redirects to login.
6. **Logout:** Remove tokens from localStorage, call revoke endpoint to invalidate refresh token server-side, notify `AuthenticationStateProvider`, redirect to landing page.
7. **Route protection:** `<AuthorizeRouteView>` checks claims. Unauthenticated users redirect to login. Users hitting `/admin/*` without Admin role see "not authorized" page.

### Google SSO

- Login page has a "Sign in with Google" button.
- Uses Google Identity Services JS library (loaded via `index.html` script tag).
- JS interop triggers Google sign-in popup -> receives Google ID token -> sends to `POST /api/users/auth/google`.
- Backend validates ID token against Google's public keys.
- If email exists: issue tokens (same as normal login).
- If email doesn't exist: auto-register user with Google claims (name, email), then issue tokens.
- Users created via Google SSO have `ExternalProvider: "Google"` flag, no password stored. Password-related UI is hidden for these users.

## Pages

### Public (no auth)

**Landing Page** (`/`)
- Hero section with CTA "Find Your Pet" -> navigates to Discover (or Login if unauthenticated).
- Brief "how it works" steps section.
- MudBlazor `MudContainer` + `MudText` + `MudButton`.

**Login** (`/login`)
- Email + password form.
- "Sign in with Google" button (Google SSO).
- Link to Register page.

**Register** (`/register`)
- Name, email, phone, password form.
- Redirects to Discover on success.

### User Experience (requires User role)

**Discover** (`/discover`)
- Card stack of available pets, loaded in paginated batches from `GET /api/pets`.
- Each card: photo placeholder (colored `MudAvatar` with initials for v1), name, species, breed, age.
- Swipe right or tap heart button = favorite (`POST /api/favorites`).
- Swipe left or tap skip button = next card.
- Filter bar at top: pet type dropdown, species filter.
- Preloads next batch when stack runs low.
- Touch gestures + mouse drag + button fallbacks on all devices.

**Pet Detail** (`/pets/{id}`)
- Full pet profile: name, species, breed, age, description, status.
- Two action buttons: "Add to Favorites" + "Reserve Now".
- Reserve triggers `POST /api/pets/{id}/reserve`.

**Favorites** (`/favorites`)
- `MudGrid` of pet cards the user has favorited.
- Each card has "View Details" and "Reserve" actions.
- Remove from favorites via icon button.

**My Reservations** (`/reservations`)
- List of reserved pets with status and date.
- Cancel reservation action per item.

**My Adoptions** (`/adoptions`)
- Read-only gallery of adopted pets, historical record.

**Profile & Settings** (`/profile`)
- Edit name, phone, preferences.
- Change password form (hidden for Google SSO users).
- Logout button.

### Admin Dashboard (requires Admin role)

**Dashboard Overview** (`/admin`)
- Stats cards row: total pets, available, reserved, adopted counts (from `GET /api/pets` with filters).
- Recent activity list: latest pet additions, reservations, adoptions (derived from pet data, no separate activity log for v1).
- Quick action buttons: "Add Pet", "View All Pets".

**Manage Pets** (`/admin/pets`)
- `MudDataGrid` with server-side pagination, sorting, filtering.
- Columns: name, species, breed, age, status, actions.
- Inline status change dropdown (Available -> Reserved -> Adopted).
- Edit button -> dialog with pet form.
- Delete button -> confirmation dialog.
- "Add Pet" button -> same form dialog in create mode.

**Pet Types** (`/admin/pet-types`)
- `MudTable` listing all pet types.
- Add/edit via inline row or dialog.
- Activate/deactivate toggle per row.

**Announcements** (`/admin/announcements`)
- `MudDataGrid` with title, status (active/scheduled/expired), start date, end date.
- Create/edit via dialog: title, body (plain text for v1), start date, end date.
- Active announcements shown as `MudAlert` banner on user-facing pages.

**Manage Users** (`/admin/users`)
- `MudDataGrid` with name, email, role, status.
- Actions: suspend/unsuspend, promote to admin.
- No user creation from admin -- users self-register.

## Component Architecture

### Layouts

- **MainLayout** -- used by public and user pages. Top app bar with logo, nav links, auth buttons. `MudAlert` banner for active announcements.
- **AdminLayout** -- extends MainLayout, adds `MudDrawer` sidebar with grouped nav sections: Dashboard, Pets, Pet Types, Announcements, Users.

### Shared Components

- **SwipeCardStack** -- manages card deck, handles touch/mouse drag gestures, emits Favorite/Skip events. Preloads next batch when stack is low.
- **PetCard** -- reusable card displaying pet summary (used in Discover, Favorites, admin grids).
- **PetFormDialog** -- `MudDialog` with pet fields, used for both create and edit in admin.
- **AnnouncementBanner** -- fetches active announcements from `GET /api/announcements/active`, renders as `MudAlert` at top of user layout.
- **ConfirmDialog** -- generic confirmation dialog for delete/cancel actions.
- **GoogleSignInButton** -- JS interop wrapper for Google Identity Services.

### State Management

No global state store for v1. Each page fetches its own data. `JwtAuthenticationStateProvider` is the single shared auth state, injected via `CascadingAuthenticationState`. Scoped services can be added later if cross-page state needs arise.

## Data Flow

```
User Action -> Page Component -> ApiClient -> HTTP -> Service API -> Domain -> MongoDB
                                    ^
                    AuthorizationMessageHandler
                    (attaches JWT, handles refresh)
```

### Key Flows

**Discover page load:**
Page -> `PetApiClient.GetPets(filter, page)` -> `GET /api/pets` -> paginated pets -> render card stack.

**Swipe right (favorite):**
SwipeCardStack emits Favorite -> Page -> `PetApiClient.AddFavorite(petId)` -> `POST /api/favorites` -> next card.

**Reserve from detail:**
PetDetail -> `PetApiClient.Reserve(petId)` -> `POST /api/pets/{id}/reserve` -> navigate to My Reservations.

**Admin edit pet:**
ManagePets -> open PetFormDialog -> submit -> `PetApiClient.UpdatePet(id, data)` -> `PUT /api/pets/{id}` -> refresh grid.

**Google SSO:**
GoogleSignInButton -> JS interop -> Google popup -> ID token -> `UserApiClient.GoogleAuth(idToken)` -> `POST /api/users/auth/google` -> store tokens -> redirect to Discover.

**Token refresh (transparent):**
Any API call -> `AuthorizationMessageHandler` checks expiry -> if expired -> `POST /api/users/refresh` -> update localStorage -> retry original request -> if refresh fails -> redirect to login.

## New Backend Work

### 1. Favorites (PetService)

New lightweight aggregate:
- **Favorite** -- properties: `Id`, `UserId`, `PetId`, `CreatedAt`.
- Factory method: `Favorite.Create(userId, petId)`.
- Repository: `IFavoriteRepository` (write), `IFavoriteQueryStore` (read).
- MongoDB collection: `favorites`.

New endpoints:
- `POST /api/favorites` -- add favorite (userId from JWT, petId in body).
- `DELETE /api/favorites/{petId}` -- remove favorite.
- `GET /api/favorites` -- get current user's favorites (paginated, returns pet details).

### 2. Announcements (PetService)

New aggregate:
- **Announcement** -- properties: `Id`, `Title`, `Body`, `StartDate`, `EndDate`, `CreatedBy`, `CreatedAt`.
- Value objects: `AnnouncementTitle`, `AnnouncementBody`.
- Factory method: `Announcement.Create(title, body, startDate, endDate, createdBy)`.
- Repository: `IAnnouncementRepository` (write), `IAnnouncementQueryStore` (read).
- MongoDB collection: `announcements`.

New endpoints:
- `POST /api/announcements` -- create (admin only).
- `PUT /api/announcements/{id}` -- update (admin only).
- `DELETE /api/announcements/{id}` -- delete (admin only).
- `GET /api/announcements` -- list all (admin, paginated).
- `GET /api/announcements/active` -- get currently active (public, for user-facing banner).

### 3. Refresh Tokens (UserService)

- New MongoDB collection: `refreshTokens` (userId, token, expiresAt, isRevoked).
- `POST /api/users/refresh` -- validate refresh token, issue new access + refresh token pair.
- Logout revokes refresh token server-side.
- Existing login endpoint updated to return refresh token alongside access token.

### 4. Google SSO (UserService)

- `POST /api/users/auth/google` -- accepts Google ID token, validates against Google's public keys.
- If email exists: issue access + refresh tokens.
- If email doesn't exist: auto-register with Google claims, then issue tokens.
- Users created via SSO get `ExternalProvider: "Google"` flag, no password stored.

## Decisions Log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Project structure | Single Blazor WASM project | Role-based routing, one deploy unit |
| UI paradigm | Tinder swipe (user) + sidebar dashboard (admin) | Engaging discovery UX + efficient admin |
| Swipe behavior | Swipe = favorite, reserve is explicit | Users browse many before committing |
| Render mode | Blazor WebAssembly | Smooth gestures, no SignalR dependency |
| Swipe input | Touch + mouse drag + button fallbacks | Accessibility, works on all devices |
| Theme | MudBlazor default dark palette | Ship fast, customize later |
| JWT storage | localStorage | Standard SPA pattern, low XSS risk in WASM |
| Token refresh | Refresh tokens (localStorage) | Better UX than re-login on expiry |
| SSO | Google only | Simplest, most common provider |
| Favorites location | PetService, own aggregate | Co-located with pet queries, clean separation |
| Announcements location | PetService | Shelter content, avoids new service overhead |
| State management | No global store | YAGNI, scoped services if needed later |
| API communication | Direct HTTP, no BFF | Simple, matches two-service architecture |
