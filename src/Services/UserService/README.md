# UserService

User management and authentication microservice for PetAdoption platform.

## Features

- **User Registration** - Create new user accounts
- **Authentication** - JWT-based authentication with BCrypt password hashing
- **Authorization** - Role-based access control (User, Admin roles)
- **Profile Management** - Update user profiles and preferences
- **User Administration** - Admin endpoints for user management
- **Event Publishing** - Publishes domain events to RabbitMQ using Outbox pattern

## Architecture

Built using Clean Architecture with the following layers:

- **Domain** - Entities, value objects, domain events, and interfaces
- **Application** - Use cases (commands/queries), DTOs, and abstractions
- **Infrastructure** - Data access (MongoDB), messaging (RabbitMQ), security (BCrypt, JWT)
- **API** - REST API controllers and configuration

## Technology Stack

- **.NET 10.0**
- **MongoDB** - Document database for persistence
- **RabbitMQ** - Message broker for event-driven communication
- **BCrypt** - Password hashing (work factor 12)
- **JWT** - Token-based authentication (HMAC SHA-256, 1-hour expiration)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker and Docker Compose (recommended)
- MongoDB (if running locally)
- RabbitMQ (if running locally)

### Running with Docker Compose

The easiest way to run the service with all dependencies:

```bash
docker-compose up -d
```

This will start:
- UserService API on `http://localhost:5001`
- MongoDB on `mongodb://localhost:27017`
- RabbitMQ on `amqp://localhost:5672` (Management UI: `http://localhost:15672`)

### Running Locally

1. Start MongoDB and RabbitMQ:
```bash
# MongoDB
docker run -d -p 27017:27017 --name mongodb -e MONGO_INITDB_ROOT_USERNAME=root -e MONGO_INITDB_ROOT_PASSWORD=example mongo:8.0

# RabbitMQ
docker run -d -p 5672:5672 -p 15672:15672 --name rabbitmq rabbitmq:4.0-management
```

2. Update `appsettings.Development.json` if needed

3. Run the application:
```bash
cd PetAdoption.UserService.API
dotnet run
```

### Running Tests

```bash
cd PetAdoption.UserService.Tests
dotnet test
```

## API Endpoints

### Public Endpoints (No Authentication)

- `POST /api/users/register` - Register a new user
- `POST /api/users/login` - Login and get JWT token

### Authenticated Endpoints (Requires JWT)

- `GET /api/users/me` - Get current user profile
- `PUT /api/users/me` - Update current user profile
- `POST /api/users/me/change-password` - Change password

### Admin Endpoints (Requires Admin Role)

- `GET /api/users` - List all users (paginated)
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users/{id}/suspend` - Suspend a user
- `POST /api/users/{id}/promote-to-admin` - Promote user to admin

## Configuration

Key configuration settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://root:example@localhost:27017"
  },
  "Database": {
    "Name": "UserDb"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "PetAdoption.UserService",
    "Audience": "PetAdoption.Services",
    "ExpirationMinutes": 60
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest"
  }
}
```

## Events Published

- `user.registered.v1` - When a new user registers
- `user.profile.updated.v1` - When user updates their profile
- `user.password.changed.v1` - When user changes password
- `user.role.changed.v1` - When user role is changed
- `user.suspended.v1` - When user is suspended

## Security

- Passwords are hashed using BCrypt with work factor 12
- JWT tokens use HMAC SHA-256 signing
- Default role is "User", "Admin" role for privileged operations
- Token expiration: 1 hour
- No token refresh mechanism (requires re-login)

## Development

### Project Structure

```
PetAdoption.UserService.API/           # REST API layer
PetAdoption.UserService.Application/   # Business logic layer
PetAdoption.UserService.Domain/        # Domain model layer
PetAdoption.UserService.Infrastructure # Data & external services
PetAdoption.UserService.Tests/         # Unit & integration tests
```

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

## License

MIT
