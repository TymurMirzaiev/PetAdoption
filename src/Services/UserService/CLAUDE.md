# UserService CLAUDE.md

## Overview

Authentication and user management service (.NET 10.0). Handles registration, JWT auth, refresh tokens, Google SSO, RBAC, profile management, and admin operations.

## Build & Test

```bash
# Run via Aspire (recommended)
dotnet run --project src/Aspire/PetAdoption.AppHost

# Run standalone
dotnet run --project src/Services/UserService/PetAdoption.UserService.API

# Tests
dotnet test tests/UserService/PetAdoption.UserService.UnitTests
dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests
```

## Aspire Integration

- References `PetAdoption.ServiceDefaults` for health checks, OpenTelemetry, resilience
- `builder.AddServiceDefaults()` in Program.cs, `app.MapDefaultEndpoints()` for `/health` and `/alive`
- RabbitMQ connection bridged via `PostConfigure<RabbitMqOptions>` (parses Aspire's AMQP URI into custom options)
- Fixed port 5001 (Blazor WASM connects directly, no service discovery)
- SQL Server connection string: `ConnectionStrings:SqlServer`

## Domain Model

### User Aggregate

- Factory: `User.Register(email, fullName, password, role, phoneNumber)`
- Factory: `User.RegisterFromGoogle(email, fullName)` — SSO users, no password, ExternalProvider="Google"
- Methods: `UpdateProfile()`, `ChangePassword()`, `PromoteToAdmin()`, `Suspend()`, `Activate()`, `RecordLogin()`
- Properties: `ExternalProvider` (string?, "Google" for SSO), `HasPassword` (bool, false for SSO users)
- Password is nullable (`Password?`) — null for SSO users
- Statuses: `Active`, `Suspended`
- Roles: `User`, `Admin`

### RefreshToken Entity

- `RefreshToken.Create(userId, lifetime)` — generates secure random token
- Methods: `Revoke()`, computed `IsValid` (not revoked and not expired)
- Stored in `RefreshTokens` table with unique index on Token

### Value Objects

- `UserId`: GUID as string, `Create()` / `From(string)`
- `Email`: validated, auto-lowercased, max 255 chars
- `FullName`: 2-100 chars, trimmed
- `Password`: `FromHash()`, `ValidatePlainText()` (8-100 chars)
- `PhoneNumber`: optional, 10-15 digits
- `UserPreferences`: preferred pet type, sizes, age range, notification settings

### Domain Events

- `UserRegisteredEvent`, `UserProfileUpdatedEvent`, `UserPasswordChangedEvent`, `UserSuspendedEvent`, `UserRoleChangedEvent`
- Published via transactional outbox → RabbitMQ (`user.events` exchange)

### Domain Exceptions

| Exception | HTTP Status |
|-----------|-------------|
| `DuplicateEmailException` | 409 Conflict |
| `InvalidCredentialsException` | 401 Unauthorized |
| `UserNotFoundException` | 404 Not Found |
| `UserSuspendedException` | 403 Forbidden |
| `ArgumentException` (value objects) | 400 Bad Request |

## Architecture

### No Mediator

UserService uses direct command/query handler injection (no mediator pattern):

```csharp
public async Task<IActionResult> Register(
    [FromBody] RegisterUserRequest request,
    [FromServices] ICommandHandler<RegisterUserCommand, RegisterUserResponse> handler)
```

### Error Handling

`ExceptionHandlingMiddleware` in `API/Middleware/` maps domain exceptions and `ArgumentException` to HTTP status codes. Pattern-matches on exception type (not error codes).

### Security

- JWT: HMAC SHA-256, configurable secret/issuer/audience
- Token claims: `sub`, `email`, `role`, `jti`, custom `userId`
- Refresh tokens: 30-day lifetime, stored in SQL Server, revoked on use (rotation)
- Password: BCrypt work factor 12
- Google SSO: validates ID token via Google's tokeninfo endpoint
- CORS: configured via `Cors:AllowedOrigins` in appsettings
- Policies: `AdminOnly` (Admin role), `UserOrAdmin` (User or Admin)

## API Endpoints

### Public (no auth)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/users/register` | Register user |
| POST | `/api/users/login` | Login, get JWT + refresh token |
| POST | `/api/users/refresh` | Refresh access token |
| POST | `/api/users/auth/google` | Google SSO (auto-registers new users) |

### Authenticated (JWT required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Get profile |
| PUT | `/api/users/me` | Update profile |
| POST | `/api/users/me/change-password` | Change password (not for SSO users) |
| POST | `/api/users/logout` | Logout (revoke refresh token) |

### Admin only

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users` | List users (paginated) |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users/{id}/suspend` | Suspend user |
| POST | `/api/users/{id}/activate` | Activate suspended user |
| POST | `/api/users/{id}/promote-to-admin` | Promote to admin |

## Database (SQL Server + EF Core)

- Database: `UserDb`
- DbContext: `UserServiceDbContext` in `Infrastructure/Persistence/`
- Tables: `Users`, `OutboxEvents`, `RefreshTokens`
- Value objects mapped via `HasConversion` (UserId→string, Email→string, FullName→string, Password→string nullable, PhoneNumber→string nullable)
- UserPreferences mapped as owned entity
- Indexes: unique Email on Users, unique Token on RefreshTokens
- LINQ is fully supported (no Filter API workaround needed)
- `EnsureCreatedAsync()` on startup (no migrations)

## Testing Notes

- Integration tests use Testcontainers SQL Server
- `AuthHelper.RegisterAndLoginAsync()` for authenticated test clients
- Admin setup requires direct SQL update (`ExecuteSqlInterpolatedAsync` to set Role to Admin)
- No seeder — tests start with empty database
