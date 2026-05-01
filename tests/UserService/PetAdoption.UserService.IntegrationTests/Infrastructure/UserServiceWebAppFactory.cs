using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PetAdoption.UserService.Infrastructure.Persistence;

namespace PetAdoption.UserService.IntegrationTests.Infrastructure;

internal class UserServiceWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public UserServiceWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
        _databaseName = $"UserServiceTest_{Guid.NewGuid():N}";
    }

    private string TestConnectionString => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString)
    {
        InitialCatalog = _databaseName
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SqlServer", TestConnectionString);

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration and re-register with test connection
            services.RemoveAll<DbContextOptions<UserServiceDbContext>>();
            services.RemoveAll<UserServiceDbContext>();

            services.AddDbContext<UserServiceDbContext>(options =>
                options.UseSqlServer(TestConnectionString));

            // Remove RabbitMQ background services (OutboxProcessorService, RabbitMqTopologySetup)
            services.RemoveAll<IHostedService>();
        });

        builder.UseEnvironment("Testing");
    }

    public UserServiceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UserServiceDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        return new UserServiceDbContext(options);
    }

    public override async ValueTask DisposeAsync()
    {
        // Clean up the test database
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
