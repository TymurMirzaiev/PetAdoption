# Blazor UI Design Spec

## Overview

A Blazor WebAssembly single-page application for the PetAdoption platform. Two UI modes: a Tinder-style swipe interface for adopters discovering pets, and a traditional dashboard for shelter administrators. Built with MudBlazor (default dark theme), authenticated via JWT with refresh tokens, calling PetService and UserService APIs directly over HTTP.

## Tech Stack

- Blazor WebAssembly (.NET 10.0, matching UserService for consistency)
- MudBlazor component framework (default dark palette)
- JWT authentication with refresh tokens (localStorage)
- Google SSO via Google Identity Services JS library
- HTTP clients to PetService (port 8080) and UserService (port 5000)
- Hosted via standalone Kestrel in the BlazorApp project; added to docker-compose alongside existing services

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

### Security Notes

- **localStorage and XSS:** While Blazor WASM reduces XSS surface (C# in WASM, not arbitrary JS), tokens in localStorage are still accessible via JS interop. Mitigate with strict Content-Security-Policy headers on the hosting server. HttpOnly cookies were considered but rejected due to BFF complexity.
- **CORS:** Both PetService and UserService must be configured with CORS to allow requests from the Blazor WASM origin. Add `AllowedOrigins` configuration in each service's `Program.cs` using `AddCors()` middleware.

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
- FullName, Email, PhoneNumber, Password fields (matching UserService `RegisterUserRequest` DTO).
- Redirects to Discover on success.

### User Experience (requires User role)

**Discover** (`/discover`)
- Card stack of available pets, loaded in paginated batches from `GET /api/pets`.
- Each card: photo placeholder (colored `MudAvatar` with initials for v1), name, pet type, breed, age.
- Swipe right or tap heart button = favorite (`POST /api/favorites`).
- Swipe left or tap skip button = next card.
- Filter bar at top: pet type dropdown.
- Preloads next batch when stack runs low.
- Touch gestures + mouse drag + button fallbacks on all devices.
- **Empty state:** "No more pets to discover" message with link to adjust filters.
- **Loading state:** Skeleton card placeholders while fetching.

**Pet Detail** (`/pets/{id}`)
- Full pet profile: name, pet type, breed, age, description, status.
- Two action buttons: "Add to Favorites" + "Reserve Now".
- Reserve triggers `POST /api/pets/{id}/reserve`.

**Favorites** (`/favorites`)
- `MudGrid` of pet cards the user has favorited.
- Each card has "View Details" and "Reserve" actions.
- Remove from favorites via icon button.
- **Empty state:** "No favorites yet. Start discovering pets!" with link to Discover.

**My Reservations** (`/reservations`)
- List of reserved pets with status and date.
- Cancel reservation action per item.
- **Empty state:** "No active reservations."

**My Adoptions** (`/adoptions`)
- Read-only gallery of adopted pets, historical record.
- **Empty state:** "No adoptions yet."

**Profile & Settings** (`/profile`)
- Edit FullName, PhoneNumber.
- Change password form (hidden for Google SSO users).
- Logout button.

### Admin Dashboard (requires Admin role)

**Dashboard Overview** (`/admin`)
- Stats cards row: total pets, available, reserved, adopted counts (from `GET /api/pets` with filters).
- Recent activity list: latest pet additions, reservations, adoptions (derived from pet data, no separate activity log for v1).
- Quick action buttons: "Add Pet", "View All Pets".

**Manage Pets** (`/admin/pets`)
- `MudDataGrid` with server-side pagination, sorting, filtering.
- Columns: name, pet type, breed, age, status, actions.
- Status action buttons per row that respect domain state machine: "Reserve" (if Available), "Adopt" (if Reserved), "Cancel Reservation" (if Reserved). These call the existing command endpoints (`/reserve`, `/adopt`, `/cancel-reservation`), not a generic status update.
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
- Actions: suspend/activate, promote to admin.
- No user creation from admin -- users self-register.

## Error Handling (UI)

- **API errors** are shown as `MudSnackbar` toast notifications with the error message from the `ErrorResponse` body.
- **409 Conflict** (e.g., pet already reserved by another user): toast with explanation, card/page refreshes to show updated status.
- **401 Unauthorized** after refresh token failure: redirect to login with "Session expired" message.
- **Network errors**: toast with "Connection error. Please try again." and a retry option where applicable.
- **Form validation errors**: inline field-level validation messages using MudBlazor's built-in form validation.

## Responsive Layout

- **User pages:** Mobile-first. Swipe cards centered, full-width on small screens. Pet grids responsive via `MudGrid` breakpoints (1 column mobile, 2 tablet, 3+ desktop).
- **Admin pages:** Desktop-primary. `MudDrawer` sidebar collapses to hamburger menu on small screens. Data grids scroll horizontally on mobile.

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

### 0. Prerequisites

- **Upgrade BlazorApp to .NET 10.0:** Currently targets `net9.0`. Upgrade `PetAdoption.Web.BlazorApp.csproj` to `net10.0` to match UserService.
- **PetService stays on .NET 9.0** for now -- services communicate over HTTP so runtime version differences are fine. Upgrade PetService separately when ready.

### 1. Pet Domain Model Extensions (PetService)

The current `Pet` aggregate has only `Id`, `Name`, `PetTypeId`, `Status`, `Version`. The UI requires additional fields. Add to the `Pet` aggregate:

- **Breed** -- new value object `PetBreed` (string, max 100 chars, optional/nullable). Distinct from PetType: PetType is "Dog", Breed is "Golden Retriever".
- **Age** -- new value object `PetAge` (int, months, must be >= 0).
- **Description** -- new value object `PetDescription` (string, max 2000 chars, optional/nullable).

Update `Pet.Create()` factory method to accept these new fields. Update existing DTOs (`PetListItemDto`, `PetDetailsDto`) to include them. Update `CreatePetCommand` and `UpdatePetCommand` accordingly.

### 2. JWT Authentication for PetService

PetService currently has no authentication. Add:

- JWT Bearer authentication middleware (same shared secret as UserService).
- `[Authorize]` on endpoints that need it: favorites endpoints, reserve/adopt/cancel-reservation.
- `[Authorize(Policy = "AdminOnly")]` on admin endpoints (pet CRUD, announcements admin).
- `[AllowAnonymous]` on public read endpoints: `GET /api/pets`, `GET /api/pets/{id}`, `GET /api/announcements/active`.
- Extract `userId` from JWT claims for favorites operations.

### 3. CORS Configuration (Both Services)

Add CORS middleware to both PetService and UserService `Program.cs`:
- Allow the Blazor WASM origin (configurable via `appsettings.json`).
- Allow `Authorization` header, `Content-Type`, and standard methods.

### 4. Favorites (PetService)

New lightweight aggregate:
- **Favorite** -- properties: `Id`, `UserId` (Guid, from JWT), `PetId` (Guid), `CreatedAt` (DateTime).
- Factory method: `Favorite.Create(userId, petId)`.
- Business rule: one favorite per (userId, petId) pair (unique index in MongoDB).
- Repository: `IFavoriteRepository` (write), `IFavoriteQueryStore` (read).
- MongoDB collection: `favorites`.

**Cross-service note:** `UserId` references a UserService entity. If a user is deleted/suspended in UserService, orphaned favorites may remain. For v1, this is acceptable -- favorites are non-critical data. Future: listen for `UserSuspendedEvent` via RabbitMQ to clean up.

New endpoints:

**`POST /api/favorites`** (requires auth)
- Request: `{ "petId": "guid" }`
- Response 201: `{ "id": "guid", "petId": "guid", "createdAt": "datetime" }`
- Error 409: pet already favorited
- Error 404: pet not found

**`DELETE /api/favorites/{petId}`** (requires auth)
- Response 204: no content
- Error 404: favorite not found

**`GET /api/favorites?page=1&pageSize=10`** (requires auth)
- Response 200: `{ "items": [{ "favoriteId": "guid", "petId": "guid", "petName": "string", "petType": "string", "breed": "string", "age": 24, "status": "Available", "createdAt": "datetime" }], "totalCount": 42, "page": 1, "pageSize": 10 }`
- Uses MongoDB `$lookup` to join favorites with pets collection for the current user.

### 5. Announcements (PetService)

New aggregate:
- **Announcement** -- properties: `Id`, `Title`, `Body`, `StartDate`, `EndDate`, `CreatedBy` (Guid), `CreatedAt`.
- Value objects: `AnnouncementTitle` (1-200 chars), `AnnouncementBody` (1-5000 chars).
- Factory method: `Announcement.Create(title, body, startDate, endDate, createdBy)`.
- Business rules: `EndDate` must be after `StartDate`. `StartDate` can be in the past (for "start immediately"). All dates stored as UTC.
- Repository: `IAnnouncementRepository` (write), `IAnnouncementQueryStore` (read).
- MongoDB collection: `announcements`.

New endpoints:

**`POST /api/announcements`** (admin only)
- Request: `{ "title": "string", "body": "string", "startDate": "datetime", "endDate": "datetime" }`
- Response 201: `{ "id": "guid" }`
- Error 400: validation failures

**`PUT /api/announcements/{id}`** (admin only)
- Request: `{ "title": "string", "body": "string", "startDate": "datetime", "endDate": "datetime" }`
- Response 200: `{ "id": "guid" }`
- Error 404: not found

**`DELETE /api/announcements/{id}`** (admin only)
- Response 204: no content
- Error 404: not found

**`GET /api/announcements?page=1&pageSize=10`** (admin only)
- Response 200: `{ "items": [{ "id": "guid", "title": "string", "startDate": "datetime", "endDate": "datetime", "status": "Active|Scheduled|Expired", "createdAt": "datetime" }], "totalCount": 5, "page": 1, "pageSize": 10 }`
- Status is computed: Active if now is between start/end, Scheduled if start is future, Expired if end is past.

**`GET /api/announcements/{id}`** (admin only)
- Response 200: `{ "id": "guid", "title": "string", "body": "string", "startDate": "datetime", "endDate": "datetime", "createdBy": "guid", "createdAt": "datetime" }`
- Error 404: not found
- Used to pre-fill the edit dialog with full body content.

**`GET /api/announcements/active`** (public, no auth)
- Response 200: `[{ "id": "guid", "title": "string", "body": "string" }]`
- Returns announcements where `StartDate <= now <= EndDate`.

### 6. Refresh Tokens (UserService)

- New MongoDB collection: `refreshTokens` -- fields: `Id`, `UserId`, `Token` (random 256-bit base64), `ExpiresAt`, `IsRevoked`, `CreatedAt`.
- Token expiry: 30 days (configurable).

**`POST /api/users/refresh`**
- Request: `{ "refreshToken": "string" }`
- Response 200: `{ "accessToken": "string", "refreshToken": "string" }` (new pair, old refresh token is revoked)
- Error 401: token invalid, expired, or revoked

**`POST /api/users/logout`** (new endpoint)
- Request: `{ "refreshToken": "string" }`
- Response 204: refresh token revoked server-side

- Existing `POST /api/users/login` response updated to include `refreshToken` alongside existing `token` field.

### 7. Google SSO (UserService)

**`POST /api/users/auth/google`**
- Request: `{ "idToken": "string" }`
- Response 200: `{ "accessToken": "string", "refreshToken": "string" }`
- Backend validates ID token using Google's public JWKS endpoint.
- If email exists: issue tokens for existing user.
- If email doesn't exist: create new user with `ExternalProvider: "Google"`, `Password: null`, then issue tokens.
- Error 401: invalid or expired Google ID token.
- Requires `Google:ClientId` in appsettings for audience validation.

### 8. Activate User Endpoint (UserService)

The domain has an `Activate()` method but no corresponding API endpoint.

**`POST /api/users/{id}/activate`** (admin only)
- Response 200: `{ "userId": "guid", "status": "Active" }`
- Error 404: user not found
- Error 409: user already active

## Decisions Log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Project structure | Single Blazor WASM project | Role-based routing, one deploy unit |
| .NET version | .NET 10.0 | Match UserService, latest stable |
| UI paradigm | Tinder swipe (user) + sidebar dashboard (admin) | Engaging discovery UX + efficient admin |
| Swipe behavior | Swipe = favorite, reserve is explicit | Users browse many before committing |
| Render mode | Blazor WebAssembly | Smooth gestures, no SignalR dependency |
| Swipe input | Touch + mouse drag + button fallbacks | Accessibility, works on all devices |
| Theme | MudBlazor default dark palette | Ship fast, customize later |
| JWT storage | localStorage with CSP headers | Standard SPA pattern, CSP mitigates XSS |
| Token refresh | Refresh tokens (localStorage, 30-day expiry) | Better UX than re-login on expiry |
| SSO | Google only | Simplest, most common provider |
| Favorites location | PetService, own aggregate | Co-located with pet queries, clean separation |
| Favorites cleanup | Accept orphans for v1 | Non-critical data, event-driven cleanup later |
| Announcements location | PetService | Shelter content, avoids new service overhead |
| State management | No global store | YAGNI, scoped services if needed later |
| API communication | Direct HTTP, no BFF | Simple, matches two-service architecture |
| Hosting | Standalone Kestrel in docker-compose | Consistent with existing service deployment |
| Admin status changes | Domain-aware action buttons, not free-form dropdown | Respect Pet aggregate state machine |
