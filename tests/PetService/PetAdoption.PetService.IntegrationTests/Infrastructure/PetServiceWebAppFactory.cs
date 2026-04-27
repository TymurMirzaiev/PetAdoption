using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.Persistence;

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

            // Replace repositories that create their own MongoClient from IConfiguration
            // so they use the test database instead
            services.RemoveAll<IPetRepository>();
            services.AddSingleton<IPetRepository>(sp =>
                new TestPetRepository(sp.GetRequiredService<IMongoDatabase>()));

            services.RemoveAll<IPetQueryStore>();
            services.AddSingleton<IPetQueryStore>(sp =>
                new TestPetQueryStore(sp.GetRequiredService<IMongoDatabase>()));

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

/// <summary>
/// Test implementation of IPetRepository that uses the injected IMongoDatabase
/// instead of creating its own MongoClient from IConfiguration.
/// </summary>
internal class TestPetRepository : IPetRepository
{
    private readonly IMongoCollection<PetAdoption.PetService.Domain.Pet> _pets;

    public TestPetRepository(IMongoDatabase database)
    {
        _pets = database.GetCollection<PetAdoption.PetService.Domain.Pet>("Pets");
    }

    public async Task<PetAdoption.PetService.Domain.Pet?> GetById(Guid id)
    {
        return await _pets.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task Add(PetAdoption.PetService.Domain.Pet pet)
    {
        await _pets.InsertOneAsync(pet);
        pet.ClearDomainEvents();
    }

    public async Task Update(PetAdoption.PetService.Domain.Pet pet)
    {
        await _pets.ReplaceOneAsync(
            p => p.Id == pet.Id,
            pet);

        pet.ClearDomainEvents();
    }
}

/// <summary>
/// Test implementation of IPetQueryStore that uses the injected IMongoDatabase
/// instead of creating its own MongoClient from IConfiguration.
/// </summary>
internal class TestPetQueryStore : IPetQueryStore
{
    private readonly IMongoCollection<PetAdoption.PetService.Domain.Pet> _pets;

    public TestPetQueryStore(IMongoDatabase database)
    {
        _pets = database.GetCollection<PetAdoption.PetService.Domain.Pet>("Pets");
    }

    public async Task<IEnumerable<PetAdoption.PetService.Domain.Pet>> GetAll()
    {
        return await _pets.Find(_ => true).ToListAsync();
    }

    public async Task<PetAdoption.PetService.Domain.Pet?> GetById(Guid id)
    {
        return await _pets.Find(p => p.Id == id).FirstOrDefaultAsync();
    }
}
