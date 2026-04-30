using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PetAdoption.PetService.Infrastructure.Persistence;

namespace PetAdoption.PetService.IntegrationTests.Infrastructure;

internal class PetServiceWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public PetServiceWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
        _databaseName = $"PetAdoptionTest_{Guid.NewGuid():N}";
    }

    private const string TestJwtSecret = "test-secret-key-minimum-32-characters-long-for-testing!";
    private const string TestJwtIssuer = "PetAdoption.UserService";
    private const string TestJwtAudience = "PetAdoption.Services";

    private string TestConnectionString => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString)
    {
        InitialCatalog = _databaseName
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", TestJwtSecret);
        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);
        builder.UseSetting("ConnectionStrings:SqlServer", TestConnectionString);

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration and re-register with test connection
            services.RemoveAll<DbContextOptions<PetServiceDbContext>>();
            services.RemoveAll<PetServiceDbContext>();

            services.AddDbContext<PetServiceDbContext>(options =>
                options.UseSqlServer(TestConnectionString));

            // Remove RabbitMQ background services
            services.RemoveAll<IHostedService>();
        });

        builder.UseEnvironment("Development");
    }

    public PetServiceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PetServiceDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        return new PetServiceDbContext(options);
    }

    public static string GenerateTestToken(string userId = "test-user-id", string role = "Admin")
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-secret-key-minimum-32-characters-long-for-testing!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("userId", userId)
        };
        var token = new JwtSecurityToken(
            issuer: "PetAdoption.UserService",
            audience: "PetAdoption.Services",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public override async ValueTask DisposeAsync()
    {
        // Clean up the test database
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
