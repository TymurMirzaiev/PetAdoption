using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class OrganizationsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _nonMemberClient = null!;

    private static readonly Guid TestOrgId = Guid.NewGuid();

    public OrganizationsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);

        // Admin client — member of the org with Admin role
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "org-admin-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        // Non-member client — authenticated but belongs to a different org
        _nonMemberClient = _factory.CreateClient();
        _nonMemberClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "other-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", Guid.NewGuid().ToString() },
                    { "orgRole", "Admin" }
                }));

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _adminClient.Dispose();
        _nonMemberClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/organizations/{orgId}/address
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAddress_AsOrgAdmin_Returns200()
    {
        // Arrange
        var request = new SetAddressRequestDto(52.52m, 13.405m, "Alexanderplatz 1", "Berlin", "BE", "Germany", "10178");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/organizations/{TestOrgId}/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SetAddressResponseDto>();
        body.Should().NotBeNull();
        body!.OrgId.Should().Be(TestOrgId);
        body.City.Should().Be("Berlin");
        body.Country.Should().Be("Germany");
    }

    [Fact]
    public async Task SetAddress_AsNonMember_Returns403()
    {
        // Arrange
        var request = new SetAddressRequestDto(52.52m, 13.405m, "Alexanderplatz 1", "Berlin", "BE", "Germany", "10178");

        // Act
        var response = await _nonMemberClient.PostAsJsonAsync(
            $"/api/organizations/{TestOrgId}/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetAddress_WithoutAuth_Returns401()
    {
        // Arrange
        var unauthClient = _factory.CreateClient();
        var request = new SetAddressRequestDto(52.52m, 13.405m, "Alexanderplatz 1", "Berlin", "BE", "Germany", "10178");

        // Act
        var response = await unauthClient.PostAsJsonAsync(
            $"/api/organizations/{TestOrgId}/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetAddress_WithInvalidLat_Returns400()
    {
        // Arrange
        var request = new SetAddressRequestDto(91m, 13.405m, "Line1", "City", "", "Germany", "");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/organizations/{TestOrgId}/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetAddress_WithInvalidLng_Returns400()
    {
        // Arrange
        var request = new SetAddressRequestDto(52m, -181m, "Line1", "City", "", "Germany", "");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/organizations/{TestOrgId}/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record SetAddressRequestDto(decimal Lat, decimal Lng, string Line1, string City, string Region, string Country, string PostalCode);
    private record SetAddressResponseDto(Guid OrgId, decimal Lat, decimal Lng, string City, string Country);
}
