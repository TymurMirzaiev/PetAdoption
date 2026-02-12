using System.Reflection;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure;

// Configure MongoDB mappings
MongoDbConfiguration.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPetRepository, PetRepository>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

builder.Services.AddMediator(Assembly.GetAssembly(typeof(Program)));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(); // Registers Swagger

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
