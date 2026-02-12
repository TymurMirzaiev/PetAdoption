using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PetDb");

builder.Services.AddDbContext<PetDbContext>(options =>
    options.UseSqlServer(connectionString));

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
