using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.DependencyInjection;
using PetAdoption.PetService.Infrastructure.Persistence;
using PetAdoption.PetService.Infrastructure.Messaging;
using PetAdoption.PetService.Infrastructure.Messaging.Configuration;
using PetAdoption.PetService.Infrastructure.BackgroundServices;
using PetAdoption.PetService.Infrastructure.Middleware;

MongoDbConfiguration.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

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

// MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("MongoDB connection string is not configured");
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("PetAdoptionDb");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Repositories
builder.Services.AddSingleton<IPetRepository, PetRepository>();
builder.Services.AddSingleton<IPetQueryStore, PetQueryStore>();
builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IPetTypeRepository, PetTypeRepository>();
builder.Services.AddSingleton<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddSingleton<IFavoriteQueryStore, FavoriteQueryStore>();
builder.Services.AddSingleton<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddSingleton<IAnnouncementQueryStore, AnnouncementQueryStore>();

// Services
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

// Background services
builder.Services.AddHostedService<RabbitMqTopologySetup>();
builder.Services.AddHostedService<OutboxProcessorService>();

// Register mediator with Application assembly to auto-discover handlers
builder.Services.AddMediator(typeof(GetAllPetsQueryHandler).Assembly);

builder.Services.AddControllers();

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
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Register seeder
builder.Services.AddTransient<PetTypeSeeder>();

var app = builder.Build();

// Seed pet types on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PetTypeSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
// Exception handling must be first to catch all exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
