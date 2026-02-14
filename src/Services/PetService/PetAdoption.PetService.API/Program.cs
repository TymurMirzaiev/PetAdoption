using System.Reflection;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.DependencyInjection;
using PetAdoption.PetService.Infrastructure.Persistence;
using PetAdoption.PetService.Infrastructure.Messaging;
using PetAdoption.PetService.Infrastructure.BackgroundServices;
using PetAdoption.PetService.Infrastructure.Middleware;

// Configure MongoDB mappings
MongoDbConfiguration.Configure();

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("PetAdoptionDb");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Repositories
builder.Services.AddSingleton<IPetRepository, PetRepository>();
builder.Services.AddSingleton<IPetQueryStore, PetQueryStore>();
builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IPetTypeRepository, PetTypeRepository>();

// Services
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

// Background services
builder.Services.AddHostedService<OutboxProcessorService>();

// Register mediator with Application assembly to auto-discover handlers
builder.Services.AddMediator(typeof(GetAllPetsQueryHandler).Assembly);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(); // Registers Swagger

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

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
