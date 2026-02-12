using System.Reflection;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure;
using PetAdoption.PetService.Infrastructure.BackgroundServices;
using PetAdoption.PetService.Infrastructure.Middleware;

// Configure MongoDB mappings
MongoDbConfiguration.Configure();

var builder = WebApplication.CreateBuilder(args);

// Repositories
builder.Services.AddSingleton<IPetRepository, PetRepository>();
builder.Services.AddSingleton<IPetQueryStore, PetQueryStore>();
builder.Services.AddSingleton<IOutboxRepository, OutboxRepository>();

// Services
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

// Background services
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddMediator(Assembly.GetAssembly(typeof(Program)));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(); // Registers Swagger

var app = builder.Build();

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
