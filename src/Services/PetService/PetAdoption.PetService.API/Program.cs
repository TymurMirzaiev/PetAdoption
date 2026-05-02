using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.API.Hubs;
using PetAdoption.PetService.API.Services;
using PetAdoption.PetService.Application.Options;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.DependencyInjection;
using PetAdoption.PetService.Infrastructure.Persistence;
using PetAdoption.PetService.Infrastructure.Messaging;
using PetAdoption.PetService.Infrastructure.Messaging.Configuration;
using PetAdoption.PetService.Infrastructure.BackgroundServices;
using PetAdoption.PetService.Infrastructure.Middleware;
using PetAdoption.PetService.Infrastructure.Services;
using PetAdoption.PetService.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// When running under Aspire, override RabbitMQ host/port from the provided connection string
builder.Services.PostConfigure<RabbitMqOptions>(options =>
    ApplyAspireRabbitMqConnectionString(options, builder.Configuration.GetConnectionString("rabbitmq")));

// SQL Server via EF Core
var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("SQL Server connection string is not configured");
builder.Services.AddDbContext<PetServiceDbContext>(options =>
    options.UseSqlServer(connectionString));

// Unit of work (resolves the same scoped PetServiceDbContext)
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PetServiceDbContext>());

// Repositories (scoped — EF Core DbContext is scoped)
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IPetQueryStore, PetQueryStore>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IPetTypeRepository, PetTypeRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IFavoriteQueryStore, FavoriteQueryStore>();
builder.Services.AddScoped<IPetSkipRepository, PetSkipRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IAnnouncementQueryStore, AnnouncementQueryStore>();
builder.Services.AddScoped<IAdoptionRequestRepository, AdoptionRequestRepository>();
builder.Services.AddScoped<IAdoptionRequestQueryStore, AdoptionRequestQueryStore>();
builder.Services.AddScoped<IPetInteractionRepository, PetInteractionRepository>();
builder.Services.AddScoped<IPetMetricsQueryStore, PetMetricsQueryStore>();
builder.Services.AddScoped<IOrgDashboardQueryStore, OrgDashboardQueryStore>();

// Organization repository
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

// Discover options and ranking service
builder.Services.Configure<DiscoverOptions>(builder.Configuration.GetSection("Discover"));
builder.Services.AddScoped<IPetRankingService, PetRankingService>();

// Services
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

// Background services
builder.Services.AddHostedService<RabbitMqTopologySetup>();
builder.Services.AddHostedService<OutboxProcessorService>();

// Register mediator with Application assembly to auto-discover handlers
builder.Services.AddMediator(typeof(CreatePetCommandHandler).Assembly);

builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatQueryStore, ChatQueryStore>();
builder.Services.AddScoped<IChatAuthorizationService, ChatAuthorizationService>();
builder.Services.AddScoped<IChatNotificationService, SignalRChatNotificationService>();

builder.Services.AddSwaggerGen(); // Registers Swagger

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured");

if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT secret must be at least 32 characters for HMAC-SHA256.");

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
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
    // Allow SignalR WebSocket clients to pass the token via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "PlatformAdmin"));
});

// Media storage
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.AddSingleton<IMediaStorage, LocalDiskMediaStorage>();

// Register seeder
builder.Services.AddTransient<PetTypeSeeder>();
builder.Services.AddScoped<OrgAuthorizationFilter>();

// Dev data seeder (development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<DevDataSeeder>();
}

var app = builder.Build();

// Ensure database is created and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PetServiceDbContext>();
    await db.Database.EnsureCreatedAsync();

    var petTypeSeeder = scope.ServiceProvider.GetRequiredService<PetTypeSeeder>();
    await petTypeSeeder.SeedAsync();

    if (app.Environment.IsDevelopment())
    {
        var devSeeder = scope.ServiceProvider.GetRequiredService<DevDataSeeder>();
        await devSeeder.SeedAsync();
    }
}

// Configure the HTTP request pipeline.
// Exception handling must be first to catch all exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// In Development the Blazor WASM client connects over http://localhost:8080 (Aspire's
// fixed http endpoint). Forcing an HTTPS redirect there sends the browser to an https
// port that isn't mapped by Aspire AND, more importantly, strips the Authorization
// header on the cross-scheme redirect — surfacing as a spurious 401 / "Failed to load pets".
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

static void ApplyAspireRabbitMqConnectionString(RabbitMqOptions options, string? connStr)
{
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
}
