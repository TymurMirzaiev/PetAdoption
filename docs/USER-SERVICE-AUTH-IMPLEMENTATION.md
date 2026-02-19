# UserService - Authentication & Authorization Implementation

## Overview

Enhanced UserService with:
- **JWT Authentication** - Token-based authentication for stateless microservices
- **RBAC** - Role-Based Access Control (User, Admin)
- **Password Management** - Secure password hashing with BCrypt
- **Authorization Policies** - Protect endpoints based on roles

---

## Architecture Changes

### Domain Layer Updates

#### New Value Objects

**TODO AUTH-1.1:** Create `Password.cs` value object
```csharp
namespace PetAdoption.UserService.Domain.ValueObjects;

public record Password
{
    public string HashedValue { get; init; }

    private Password(string hashedValue) => HashedValue = hashedValue;

    // Factory method - accepts already hashed password
    public static Password FromHash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Password hash cannot be empty", nameof(hashedPassword));

        return new Password(hashedPassword);
    }

    // For creating new password (will be hashed by infrastructure)
    public static Password CreateNew(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainTextPassword));

        if (plainTextPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters", nameof(plainTextPassword));

        if (plainTextPassword.Length > 100)
            throw new ArgumentException("Password cannot exceed 100 characters", nameof(plainTextPassword));

        // Return placeholder - actual hashing happens in infrastructure
        return new Password(plainTextPassword);
    }
}
```

**TODO AUTH-1.2:** Create `UserRole.cs` enum
```csharp
namespace PetAdoption.UserService.Domain.Enums;

public enum UserRole
{
    User = 0,     // Default role
    Admin = 1     // Can manage challenges, approve adoptions, etc.
}
```

#### Updated User Aggregate

**TODO AUTH-1.3:** Update `User.cs` to include Password and Role
```csharp
namespace PetAdoption.UserService.Domain.Entities;

using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Events;

public class User
{
    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public FullName FullName { get; private set; }
    public Password Password { get; private set; }  // NEW
    public UserRole Role { get; private set; }       // NEW
    public PhoneNumber? PhoneNumber { get; private set; }
    public UserPreferences Preferences { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }  // NEW

    private readonly List<DomainEventBase> _domainEvents = new();
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    private User() { }

    // Updated factory method - now requires password
    public static User Register(
        string email,
        string fullName,
        string hashedPassword,  // Already hashed by infrastructure
        string? phoneNumber = null,
        UserRole role = UserRole.User)  // Default to User role
    {
        var user = new User
        {
            Id = UserId.Create(),
            Email = Email.From(email),
            FullName = FullName.From(fullName),
            Password = Password.FromHash(hashedPassword),
            Role = role,
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber),
            Preferences = UserPreferences.Default(),
            Status = UserStatus.Active,
            RegisteredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id.Value,
            user.Email.Value,
            user.FullName.Value,
            user.Role.ToString(),
            user.RegisteredAt
        ));

        return user;
    }

    // NEW: Update password
    public void ChangePassword(string newHashedPassword)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("Cannot change password of suspended user");

        Password = Password.FromHash(newHashedPassword);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedEvent(
            Id.Value,
            UpdatedAt
        ));
    }

    // NEW: Promote to admin
    public void PromoteToAdmin()
    {
        if (Role == UserRole.Admin)
            throw new InvalidOperationException("User is already an admin");

        Role = UserRole.Admin;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRoleChangedEvent(
            Id.Value,
            UserRole.Admin.ToString(),
            UpdatedAt
        ));
    }

    // NEW: Record successful login
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        // Don't raise event for login (too noisy), just track timestamp
    }

    // Existing methods...
    public void UpdateProfile(string? fullName = null, string? phoneNumber = null, UserPreferences? preferences = null)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("Cannot update profile of suspended user");

        var hasChanges = false;

        if (fullName != null)
        {
            FullName = FullName.From(fullName);
            hasChanges = true;
        }

        if (phoneNumber != null)
        {
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber);
            hasChanges = true;
        }

        if (preferences != null)
        {
            Preferences = preferences;
            hasChanges = true;
        }

        if (hasChanges)
        {
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserProfileUpdatedEvent(
                Id.Value,
                fullName,
                phoneNumber,
                UpdatedAt
            ));
        }
    }

    public void Suspend(string reason)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("User is already suspended");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserSuspendedEvent(
            Id.Value,
            reason,
            UpdatedAt
        ));
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(DomainEventBase @event)
    {
        _domainEvents.Add(@event);
    }
}
```

#### New Domain Events

**TODO AUTH-1.4:** Create `UserPasswordChangedEvent.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserPasswordChangedEvent(
    string UserId,
    DateTime ChangedAt
) : DomainEventBase;
```

**TODO AUTH-1.5:** Create `UserRoleChangedEvent.cs`
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserRoleChangedEvent(
    string UserId,
    string NewRole,
    DateTime ChangedAt
) : DomainEventBase;
```

**TODO AUTH-1.6:** Update `UserRegisteredEvent.cs` to include Role
```csharp
namespace PetAdoption.UserService.Domain.Events;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    string FullName,
    string Role,  // NEW
    DateTime RegisteredAt
) : DomainEventBase;
```

#### New Domain Exceptions

**TODO AUTH-1.7:** Create `InvalidCredentialsException.cs`
```csharp
namespace PetAdoption.UserService.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "Invalid email or password")
    {
    }
}
```

**TODO AUTH-1.8:** Create `UserSuspendedException.cs`
```csharp
namespace PetAdoption.UserService.Domain.Exceptions;

public class UserSuspendedException : DomainException
{
    public UserSuspendedException(string userId)
        : base("USER_SUSPENDED", $"User '{userId}' is suspended and cannot login")
    {
    }
}
```

---

### Application Layer Updates

#### New Abstractions

**TODO AUTH-2.1:** Create `IPasswordHasher.cs` interface
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string plainTextPassword);
    bool VerifyPassword(string plainTextPassword, string hashedPassword);
}
```

**TODO AUTH-2.2:** Create `IJwtTokenGenerator.cs` interface
```csharp
namespace PetAdoption.UserService.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(string userId, string email, string role);
}
```

#### New Commands

**TODO AUTH-2.3:** Create `LoginCommand.cs`
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResponse>;
```

**TODO AUTH-2.4:** Create `LoginCommandHandler.cs`
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        // Find user by email
        var email = Email.From(command.Email);
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            // Don't reveal whether user exists
            throw new InvalidCredentialsException();
        }

        // Check if user is suspended
        if (user.Status == Domain.Enums.UserStatus.Suspended)
        {
            throw new UserSuspendedException(user.Id.Value);
        }

        // Verify password
        var isPasswordValid = _passwordHasher.VerifyPassword(
            command.Password,
            user.Password.HashedValue
        );

        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(
            user.Id.Value,
            user.Email.Value,
            user.Role.ToString()
        );

        // Record login
        user.RecordLogin();
        await _userRepository.SaveAsync(user);

        return new LoginResponse(
            Success: true,
            Token: token,
            UserId: user.Id.Value,
            Email: user.Email.Value,
            FullName: user.FullName.Value,
            Role: user.Role.ToString(),
            ExpiresIn: 3600 // 1 hour in seconds
        );
    }
}
```

**TODO AUTH-2.5:** Update `RegisterUserCommandHandler.cs` to hash password
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;  // NEW

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)  // NEW
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;  // NEW
    }

    public async Task<RegisterUserResponse> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var email = Email.From(command.Email);
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
        {
            throw new DuplicateEmailException(command.Email);
        }

        // Hash password
        var hashedPassword = _passwordHasher.HashPassword(command.Password);  // NEW

        // Create new user
        var user = User.Register(
            command.Email,
            command.FullName,
            hashedPassword,  // NEW
            command.PhoneNumber
        );

        // Save user (will publish events via outbox)
        await _userRepository.SaveAsync(user);

        return new RegisterUserResponse(
            Success: true,
            UserId: user.Id.Value,
            Message: "User registered successfully"
        );
    }
}
```

**TODO AUTH-2.6:** Create `ChangePasswordCommand.cs` and handler
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<ChangePasswordResponse>;

public record ChangePasswordResponse(bool Success, string Message);

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResponse> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        // Verify current password
        var isCurrentPasswordValid = _passwordHasher.VerifyPassword(
            command.CurrentPassword,
            user.Password.HashedValue
        );

        if (!isCurrentPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        // Hash and set new password
        var newHashedPassword = _passwordHasher.HashPassword(command.NewPassword);
        user.ChangePassword(newHashedPassword);

        await _userRepository.SaveAsync(user);

        return new ChangePasswordResponse(true, "Password changed successfully");
    }
}
```

**TODO AUTH-2.7:** Create `PromoteToAdminCommand.cs` and handler
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record PromoteToAdminCommand(string UserId) : ICommand<PromoteToAdminResponse>;

public record PromoteToAdminResponse(bool Success, string Message);

public class PromoteToAdminCommandHandler : ICommandHandler<PromoteToAdminCommand, PromoteToAdminResponse>
{
    private readonly IUserRepository _userRepository;

    public PromoteToAdminCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PromoteToAdminResponse> HandleAsync(
        PromoteToAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        user.PromoteToAdmin();
        await _userRepository.SaveAsync(user);

        return new PromoteToAdminResponse(true, "User promoted to admin successfully");
    }
}
```

#### Updated DTOs

**TODO AUTH-2.8:** Update `RegisterUserCommand.cs` to include password
```csharp
namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record RegisterUserCommand(
    string Email,
    string FullName,
    string Password,      // NEW - required
    string? PhoneNumber = null
) : ICommand<RegisterUserResponse>;
```

**TODO AUTH-2.9:** Create `LoginResponse.cs`
```csharp
namespace PetAdoption.UserService.Application.DTOs;

public record LoginResponse(
    bool Success,
    string Token,
    string UserId,
    string Email,
    string FullName,
    string Role,
    int ExpiresIn  // Seconds until token expires
);
```

**TODO AUTH-2.10:** Update `UserDto.cs` to include Role
```csharp
namespace PetAdoption.UserService.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Status,
    string Role,  // NEW
    UserPreferencesDto Preferences,
    DateTime RegisteredAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt  // NEW
);

public record UserPreferencesDto(
    string? PreferredPetType,
    List<string>? PreferredSizes,
    string? PreferredAgeRange,
    bool ReceiveEmailNotifications,
    bool ReceiveSmsNotifications
);
```

---

### Infrastructure Layer Updates

#### Password Hashing

**TODO AUTH-3.1:** Add BCrypt NuGet package
```bash
cd src/Services/UserService/PetAdoption.UserService.Infrastructure
dotnet add package BCrypt.Net-Next
```

**TODO AUTH-3.2:** Create `BCryptPasswordHasher.cs`
```csharp
namespace PetAdoption.UserService.Infrastructure.Security;

using PetAdoption.UserService.Application.Abstractions;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
    }

    public bool VerifyPassword(string plainTextPassword, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
```

#### JWT Token Generation

**TODO AUTH-3.3:** Add JWT NuGet packages
```bash
cd src/Services/UserService/PetAdoption.UserService.Infrastructure
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

**TODO AUTH-3.4:** Create `JwtOptions.cs`
```csharp
namespace PetAdoption.UserService.Infrastructure.Security;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
```

**TODO AUTH-3.5:** Create `JwtTokenGenerator.cs`
```csharp
namespace PetAdoption.UserService.Infrastructure.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetAdoption.UserService.Application.Abstractions;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken(string userId, string email, string role)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Secret)
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", userId)  // Custom claim for easy access
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### Dependency Injection Updates

**TODO AUTH-3.6:** Update `ServiceCollectionExtensions.cs`
```csharp
namespace PetAdoption.UserService.Infrastructure.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Infrastructure.Security;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Security services
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // JWT configuration
        services.Configure<JwtOptions>(
            configuration.GetSection("Jwt")
        );

        // ... existing MongoDB, RabbitMQ, repository registrations ...

        return services;
    }
}
```

---

### API Layer Updates

#### Authentication Configuration

**TODO AUTH-4.1:** Update `appsettings.json` to include JWT settings
```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://root:example@localhost:27017"
  },
  "Database": {
    "Name": "UserDb"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-characters-long-change-in-production",
    "Issuer": "PetAdoption.UserService",
    "Audience": "PetAdoption.Services",
    "ExpirationMinutes": 60
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Exchanges": [
      {
        "Name": "user.events",
        "Type": "topic",
        "Durable": true
      }
    ],
    "Queues": []
  }
}
```

**TODO AUTH-4.2:** Update `Program.cs` to configure JWT authentication
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using PetAdoption.UserService.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("MongoDB connection string is not configured");
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(
    builder.Configuration["Database:Name"] ?? "UserDb"
);
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Infrastructure services (includes password hasher, JWT generator, repositories, RabbitMQ)
builder.Services.AddInfrastructure(builder.Configuration);

// Register command/query handlers
// ... (register all handlers)

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)
        ),
        ClockSkew = TimeSpan.Zero  // No grace period for token expiration
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UserService API", Version = "v1" });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Important: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

#### Updated Controller

**TODO AUTH-4.3:** Update `UsersController.cs` with authentication endpoints and authorization
```csharp
namespace PetAdoption.UserService.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Application.Queries;
using System.Security.Claims;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    // PUBLIC ENDPOINTS (No authentication required)

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] ICommandHandler<RegisterUserCommand, RegisterUserResponse> handler)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.FullName,
            request.Password,
            request.PhoneNumber
        );

        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Login (sign in) with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, LoginResponse> handler)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    // AUTHENTICATED ENDPOINTS (Require valid JWT token)

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]  // Any authenticated user
    public async Task<IActionResult> GetMyProfile(
        [FromServices] IQueryHandler<GetUserByIdQuery, UserDto> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var query = new GetUserByIdQuery(userId);
        var user = await handler.HandleAsync(query);

        return Ok(user);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResponse> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var command = new UpdateUserProfileCommand(
            userId,
            request.FullName,
            request.PhoneNumber,
            request.Preferences
        );

        var response = await handler.HandleAsync(command);
        return Ok(response);
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var command = new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );

        var response = await handler.HandleAsync(command);
        return Ok(response);
    }

    // ADMIN-ONLY ENDPOINTS

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserById(
        string id,
        [FromServices] IQueryHandler<GetUserByIdQuery, UserDto> handler)
    {
        var query = new GetUserByIdQuery(id);
        var user = await handler.HandleAsync(query);

        return Ok(user);
    }

    /// <summary>
    /// List all users (Admin only, paginated)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromServices] IQueryHandler<GetUsersQuery, GetUsersResponse> handler)
    {
        var query = new GetUsersQuery(skip, take);
        var response = await handler.HandleAsync(query);

        return Ok(response);
    }

    /// <summary>
    /// Suspend a user (Admin only)
    /// </summary>
    [HttpPost("{id}/suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SuspendUser(
        string id,
        [FromBody] SuspendUserRequest request,
        [FromServices] ICommandHandler<SuspendUserCommand, SuspendUserResponse> handler)
    {
        var command = new SuspendUserCommand(id, request.Reason);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Promote user to admin (Admin only)
    /// </summary>
    [HttpPost("{id}/promote-to-admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PromoteToAdmin(
        string id,
        [FromServices] ICommandHandler<PromoteToAdminCommand, PromoteToAdminResponse> handler)
    {
        var command = new PromoteToAdminCommand(id);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }
}

// Request DTOs
public record RegisterUserRequest(
    string Email,
    string FullName,
    string Password,
    string? PhoneNumber
);

public record LoginRequest(string Email, string Password);

public record UpdateProfileRequest(
    string? FullName,
    string? PhoneNumber,
    UserPreferencesDto? Preferences
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record SuspendUserRequest(string Reason);
```

---

## Database Seeding (Create First Admin)

**TODO AUTH-5.1:** Create `DatabaseSeeder.cs`
```csharp
namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

public class DatabaseSeeder
{
    private readonly IMongoDatabase _database;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(IMongoDatabase database, IPasswordHasher passwordHasher)
    {
        _database = database;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        var usersCollection = _database.GetCollection<User>("Users");

        // Check if admin already exists
        var adminEmail = "admin@petadoption.com";
        var existingAdmin = await usersCollection
            .Find(u => u.Email.Value == adminEmail)
            .FirstOrDefaultAsync();

        if (existingAdmin == null)
        {
            // Create first admin user
            var hashedPassword = _passwordHasher.HashPassword("Admin123!");  // CHANGE IN PRODUCTION

            var admin = User.Register(
                adminEmail,
                "System Administrator",
                hashedPassword,
                role: UserRole.Admin
            );

            await usersCollection.InsertOneAsync(admin);

            Console.WriteLine($"✅ Admin user created: {adminEmail}");
            Console.WriteLine("⚠️  Default password: Admin123! - CHANGE IMMEDIATELY");
        }
    }
}
```

**TODO AUTH-5.2:** Run seeder in `Program.cs`
```csharp
// After building the app, before app.Run()

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var seeder = new DatabaseSeeder(database, passwordHasher);
    await seeder.SeedAsync();
}

app.Run();
```

---

## Testing the Authentication Flow

### 1. Register a new user
```bash
curl -X POST http://localhost:8081/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "fullName": "John Doe",
    "password": "Password123!"
  }'
```

Response:
```json
{
  "success": true,
  "userId": "abc123...",
  "message": "User registered successfully"
}
```

### 2. Login to get JWT token
```bash
curl -X POST http://localhost:8081/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "Password123!"
  }'
```

Response:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "abc123...",
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": "User",
  "expiresIn": 3600
}
```

### 3. Access protected endpoint (use token)
```bash
curl -X GET http://localhost:8081/api/users/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 4. Login as admin
```bash
curl -X POST http://localhost:8081/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@petadoption.com",
    "password": "Admin123!"
  }'
```

### 5. Access admin endpoint
```bash
curl -X GET http://localhost:8081/api/users \
  -H "Authorization: Bearer <admin-token>"
```

---

## JWT Token Structure

When decoded, the JWT token contains:
```json
{
  "sub": "user-id-123",
  "email": "john@example.com",
  "role": "User",
  "userId": "user-id-123",
  "jti": "unique-token-id",
  "exp": 1709123456,
  "iss": "PetAdoption.UserService",
  "aud": "PetAdoption.Services"
}
```

---

## Using JWT in Other Services

**In ChallengeService, DistributionService, etc.:**

1. Add same JWT configuration to `appsettings.json`
2. Configure JWT authentication in `Program.cs` (same as UserService)
3. Use `[Authorize]` attributes
4. Extract user info from claims:

```csharp
var userId = User.FindFirstValue("userId");
var email = User.FindFirstValue(ClaimTypes.Email);
var role = User.FindFirstValue(ClaimTypes.Role);
var isAdmin = User.IsInRole("Admin");
```

---

## Updated Task List

### **NEW AUTH TASKS:**

**Task #9: Implement Authentication & Authorization** ⬜
- Add Password value object and Role enum
- Update User aggregate with password, role, login tracking
- Create new domain events (PasswordChanged, RoleChanged)
- Implement BCryptPasswordHasher
- Implement JwtTokenGenerator
- Create LoginCommand + handler
- Create ChangePasswordCommand + handler
- Create PromoteToAdminCommand + handler
- Update RegisterUserCommand to include password
- Configure JWT in Program.cs
- Update UsersController with auth endpoints
- Add [Authorize] attributes to protected endpoints
- Create database seeder for first admin
- **Estimated Effort:** 6-8 hours

**Dependencies:**
- Build on top of Tasks #1-#5
- Can be done after basic UserService works

---

## Security Considerations

### Password Requirements
- Minimum 8 characters
- Maximum 100 characters
- Consider adding: uppercase, lowercase, number, special char validation

### JWT Security
- ✅ HTTPS only in production
- ✅ Secure secret (min 32 characters, store in environment variable)
- ✅ Short expiration (1 hour)
- ✅ Validate issuer and audience
- ✅ No grace period for expiration (ClockSkew = 0)

### Best Practices
- ⚠️ Change default admin password immediately
- ⚠️ Store JWT secret in environment variables (not appsettings.json)
- ⚠️ Use HTTPS in production
- ⚠️ Implement refresh tokens for longer sessions (future enhancement)
- ⚠️ Add rate limiting on login endpoint (prevent brute force)
- ⚠️ Log failed login attempts
- ⚠️ Consider adding email verification (future enhancement)

---

## API Endpoints Summary

| Endpoint | Method | Auth | Role | Description |
|----------|--------|------|------|-------------|
| `/api/users/register` | POST | No | - | Register new user |
| `/api/users/login` | POST | No | - | Login and get JWT token |
| `/api/users/me` | GET | Yes | Any | Get current user profile |
| `/api/users/me` | PUT | Yes | Any | Update current user profile |
| `/api/users/me/change-password` | POST | Yes | Any | Change password |
| `/api/users/{id}` | GET | Yes | Admin | Get user by ID |
| `/api/users` | GET | Yes | Admin | List all users |
| `/api/users/{id}/suspend` | POST | Yes | Admin | Suspend user |
| `/api/users/{id}/promote-to-admin` | POST | Yes | Admin | Promote to admin |

---

## Next Steps

1. ✅ Implement Tasks #1-#5 (basic UserService)
2. ✅ Add Task #9 (Authentication & Authorization)
3. ✅ Test complete auth flow
4. ✅ Seed first admin user
5. ➡️ Use JWT authentication in all other services

**Default Admin Credentials:**
- Email: `admin@petadoption.com`
- Password: `Admin123!` (⚠️ CHANGE IMMEDIATELY)

---

**Last Updated:** 2026-02-20
**Status:** Ready for Implementation
