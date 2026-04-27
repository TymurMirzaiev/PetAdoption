using Testcontainers.MongoDb;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
