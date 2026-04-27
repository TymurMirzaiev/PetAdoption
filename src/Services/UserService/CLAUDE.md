# UserService CLAUDE.md

## Overview

Authentication and user management service (.NET 10.0). Handles registration, JWT auth, RBAC, profile management, and admin operations.

## Build & Test

```bash
cd src/Services/UserService && docker-compose up -d --build
dotnet test tests/UserService/PetAdoption.UserService.UnitTests
dotnet test tests/UserService/PetAdoption.UserService.IntegrationTests
```

## Domain Model

### User Aggregate

- Factory: `User.Register(email, fullName, password, role, phoneNumber, preferences)`
- Methods: `UpdateProfile()`, `ChangePassword()`, `PromoteToAdmin()`, `Suspend()`, `Activate()`, `RecordLogin()`
- Statuses: `Active`, `Suspended`
- Roles: `User`, `Admin`

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
- Password: BCrypt work factor 12
- Policies: `AdminOnly` (Admin role), `UserOrAdmin` (User or Admin)

## API Endpoints

### Public (no auth)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/users/register` | Register user |
| POST | `/api/users/login` | Login, get JWT |

### Authenticated (JWT required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Get profile |
| PUT | `/api/users/me` | Update profile |
| POST | `/api/users/me/change-password` | Change password |

### Admin only

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users` | List users (paginated) |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users/{id}/suspend` | Suspend user |
| POST | `/api/users/{id}/promote-to-admin` | Promote to admin |

## MongoDB

- Database: `UserDb`
- Collections: `Users`, `OutboxEvents`
- Custom serializers for all value objects in `Infrastructure/Persistence/Serializers/`
- **Always use Filter API, never LINQ** (see root CLAUDE.md)

## Testing Notes

- Integration tests use Testcontainers MongoDB
- `AuthHelper.RegisterAndLoginAsync()` for authenticated test clients
- Admin setup requires direct MongoDB update (set `Role` to `1` in `Users` collection)
- No seeder — tests start with empty database
