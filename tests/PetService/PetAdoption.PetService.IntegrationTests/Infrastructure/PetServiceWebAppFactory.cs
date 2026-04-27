using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

internal class PetServiceWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _mongoConnectionString;
    private readonly string _databaseName;

    public PetServiceWebAppFactory(string mongoConnectionString)
    {
        _mongoConnectionString = mongoConnectionString;
        _databaseName = $"PetAdoptionTest_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real MongoDB registrations
            services.RemoveAll<IMongoDatabase>();

            // Register test MongoDB
            var mongoClient = new MongoClient(_mongoConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(_databaseName);
            services.AddSingleton<IMongoDatabase>(mongoDatabase);

            // Remove RabbitMQ background services
            services.RemoveAll<IHostedService>();
        });

        builder.UseEnvironment("Development");
    }

    public IMongoDatabase GetTestDatabase()
    {
        var client = new MongoClient(_mongoConnectionString);
        return client.GetDatabase(_databaseName);
    }

    public override async ValueTask DisposeAsync()
    {
        // Clean up the test database
        var client = new MongoClient(_mongoConnectionString);
        await client.DropDatabaseAsync(_databaseName);
        await base.DisposeAsync();
    }
}
