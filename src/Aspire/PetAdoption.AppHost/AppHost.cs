var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent);

var petDb = sql.AddDatabase("PetAdoptionDb");
var userDb = sql.AddDatabase("UserDb");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// Shared JWT configuration
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

// PetService API (.NET 9.0) — port 8080 to match Blazor WASM appsettings
var petService = builder.AddProject<Projects.PetAdoption_PetService_API>("petservice")
    .WithReference(petDb, connectionName: "SqlServer")
    .WithReference(rabbitmq)
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", "PetAdoption.UserService")
    .WithEnvironment("Jwt__Audience", "PetAdoption.Services")
    .WaitFor(sql)
    .WaitFor(rabbitmq);

// UserService API (.NET 10.0) — port 5001 to match Blazor WASM appsettings
var userService = builder.AddProject<Projects.PetAdoption_UserService_API>("userservice")
    .WithReference(userDb, connectionName: "SqlServer")
    .WithReference(rabbitmq)
    .WithHttpEndpoint(port: 5001, name: "http")
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", "PetAdoption.UserService")
    .WithEnvironment("Jwt__Audience", "PetAdoption.Services")
    .WaitFor(sql)
    .WaitFor(rabbitmq);

// Blazor WASM Frontend (standalone client app, no service discovery needed)
builder.AddProject<Projects.PetAdoption_Web_BlazorApp>("blazorapp")
    .WithExternalHttpEndpoints();

builder.Build().Run();
