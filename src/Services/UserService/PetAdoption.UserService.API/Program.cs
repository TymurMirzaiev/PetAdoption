using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetAdoption.UserService.Infrastructure.DependencyInjection;
using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using PetAdoption.UserService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Infrastructure services (SQL Server, RabbitMQ, Repositories, Security, Handlers)
builder.Services.AddInfrastructure(builder.Configuration);

// When running under Aspire, override RabbitMQ host/port from the provided connection string
builder.Services.PostConfigure<RabbitMqOptions>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("rabbitmq");
    if (connStr is not null && Uri.TryCreate(connStr, UriKind.Absolute, out var uri))
    {
        options.Host = uri.Host;
        options.Port = uri.Port > 0 ? uri.Port : 5672;
        if (uri.UserInfo is { Length: > 0 } userInfo)
        {
            var parts = userInfo.Split(':');
            options.User = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1) options.Password = Uri.UnescapeDataString(parts[1]);
        }
    }
});

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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "PlatformAdmin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin", "PlatformAdmin"));
    options.AddPolicy("PlatformAdminOnly", policy => policy.RequireRole("PlatformAdmin"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddControllers();

// Add OpenAPI/Swagger support (.NET 10 native approach)
builder.Services.AddOpenApi();

// Dev data seeder (development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<DevDataSeeder>();
}

var app = builder.Build();

// Ensure database is created and seed dev data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DevDataSeeder>();
        await seeder.SeedAsync();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Exception handling middleware must be first to catch all exceptions
app.UseMiddleware<PetAdoption.UserService.API.Middleware.ExceptionHandlingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Logger.LogInformation("UserService API starting...");

app.Run();
