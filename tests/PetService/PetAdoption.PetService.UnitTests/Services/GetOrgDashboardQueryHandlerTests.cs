using FluentAssertions;
using Moq;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Services;

public class GetOrgDashboardQueryHandlerTests
{
    private readonly Mock<IOrgDashboardQueryStore> _storeMock = new();

    // ──────────────────────────────────────────────────────────────
    // GetOrgDashboardQueryHandler — AdoptionRate
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAdoptedPets_ShouldComputeAdoptionRateCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 10,
                AvailablePets: 5,
                ReservedPets: 2,
                AdoptedPets: 3,
                TotalRequests: 5,
                PendingRequests: 2,
                TotalImpressions: 100,
                TotalSwipes: 20));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.AdoptionRate.Should().Be(30.0);
    }

    [Fact]
    public async Task Handle_WithZeroTotalPets_ShouldReturnZeroAdoptionRate()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 0,
                AvailablePets: 0,
                ReservedPets: 0,
                AdoptedPets: 0,
                TotalRequests: 0,
                PendingRequests: 0,
                TotalImpressions: 0,
                TotalSwipes: 0));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.AdoptionRate.Should().Be(0.0);
    }

    // ──────────────────────────────────────────────────────────────
    // GetOrgDashboardQueryHandler — AvgSwipeRate
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithImpressions_ShouldComputeAvgSwipeRateCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 10,
                AvailablePets: 8,
                ReservedPets: 1,
                AdoptedPets: 1,
                TotalRequests: 3,
                PendingRequests: 1,
                TotalImpressions: 200,
                TotalSwipes: 50));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.AvgSwipeRate.Should().Be(25.0);
    }

    [Fact]
    public async Task Handle_WithZeroImpressions_ShouldReturnZeroAvgSwipeRate()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 5,
                AvailablePets: 5,
                ReservedPets: 0,
                AdoptedPets: 0,
                TotalRequests: 0,
                PendingRequests: 0,
                TotalImpressions: 0,
                TotalSwipes: 0));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.AvgSwipeRate.Should().Be(0.0);
    }

    [Fact]
    public async Task Handle_WithRoundableRate_ShouldRoundToOneDecimalPlace()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 3,
                AvailablePets: 1,
                ReservedPets: 1,
                AdoptedPets: 1,
                TotalRequests: 1,
                PendingRequests: 0,
                TotalImpressions: 300,
                TotalSwipes: 100));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.AdoptionRate.Should().Be(33.3);
        result.AvgSwipeRate.Should().Be(33.3);
    }

    // ──────────────────────────────────────────────────────────────
    // GetOrgDashboardQueryHandler — Response fields
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldPassThroughAllCountFields()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _storeMock.Setup(s => s.GetDashboardAsync(orgId, default))
            .ReturnsAsync(new OrgDashboardData(
                TotalPets: 7,
                AvailablePets: 4,
                ReservedPets: 2,
                AdoptedPets: 1,
                TotalRequests: 8,
                PendingRequests: 3,
                TotalImpressions: 500,
                TotalSwipes: 100));

        var handler = new GetOrgDashboardQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardQuery(orgId));

        // Assert
        result.TotalPets.Should().Be(7);
        result.AvailablePets.Should().Be(4);
        result.ReservedPets.Should().Be(2);
        result.AdoptedPets.Should().Be(1);
        result.TotalAdoptionRequests.Should().Be(8);
        result.PendingRequests.Should().Be(3);
        result.TotalImpressions.Should().Be(500);
    }
}
