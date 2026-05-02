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

public class GetOrgDashboardTrendsQueryHandlerTests
{
    private readonly Mock<IOrgDashboardQueryStore> _storeMock = new();

    // ──────────────────────────────────────────────────────────────
    // Date defaulting
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithNullFromAndTo_ShouldDefaultToLast12Weeks()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var before = DateTime.UtcNow.AddDays(-84).AddSeconds(-5);
        var after = DateTime.UtcNow.AddSeconds(5);

        _storeMock.Setup(s => s.GetTrendsAsync(
                orgId,
                It.IsInRange(before, after, Moq.Range.Inclusive),
                It.IsInRange(before, after, Moq.Range.Inclusive),
                default))
            .ReturnsAsync(new OrgDashboardTrendsData(
                new List<(int, int)>(),
                new List<(int, int)>()));

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardTrendsQuery(orgId, null, null));

        // Assert
        result.Should().NotBeNull();
        _storeMock.Verify(s => s.GetTrendsAsync(
            orgId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            default), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Validation: from >= to
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithFromAfterTo_ShouldThrowDomainException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var from = DateTime.UtcNow;
        var to = from.AddDays(-1);

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var act = () => handler.Handle(new GetOrgDashboardTrendsQuery(orgId, from, to));

        // Assert
        (await act.Should().ThrowAsync<DomainException>())
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOperation);
    }

    [Fact]
    public async Task Handle_WithFromEqualToTo_ShouldThrowDomainException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var date = DateTime.UtcNow;

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var act = () => handler.Handle(new GetOrgDashboardTrendsQuery(orgId, date, date));

        // Assert
        (await act.Should().ThrowAsync<DomainException>())
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOperation);
    }

    // ──────────────────────────────────────────────────────────────
    // 52-week clamping
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithRangeOver364Days_ShouldClampFrom()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var to = DateTime.UtcNow;
        var from = to.AddDays(-500);

        DateTime capturedFrom = default;
        _storeMock.Setup(s => s.GetTrendsAsync(orgId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .Callback<Guid, DateTime, DateTime, CancellationToken>((_, f, _, _) => capturedFrom = f)
            .ReturnsAsync(new OrgDashboardTrendsData(
                new List<(int, int)>(),
                new List<(int, int)>()));

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        await handler.Handle(new GetOrgDashboardTrendsQuery(orgId, from, to));

        // Assert
        capturedFrom.Should().BeCloseTo(to.AddDays(-364), TimeSpan.FromSeconds(5));
    }

    // ──────────────────────────────────────────────────────────────
    // Zero-fill dense list
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithSparseData_ShouldProduceDenseListWithZeroFill()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-28); // 4 weeks
        var to = DateTime.UtcNow;

        // Only week 1 has adoptions, only week 3 has requests
        _storeMock.Setup(s => s.GetTrendsAsync(orgId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new OrgDashboardTrendsData(
                new List<(int WeekOffset, int Count)> { (1, 3) },
                new List<(int WeekOffset, int Count)> { (3, 5) }));

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardTrendsQuery(orgId, from, to));

        // Assert
        var expectedWeeks = (int)(to - from).TotalDays / 7 + 1;
        result.AdoptionsByWeek.Should().HaveCount(expectedWeeks);
        result.RequestsByWeek.Should().HaveCount(expectedWeeks);

        result.AdoptionsByWeek[0].Count.Should().Be(0);
        result.AdoptionsByWeek[1].Count.Should().Be(3);
        result.AdoptionsByWeek[2].Count.Should().Be(0);

        result.RequestsByWeek[0].Count.Should().Be(0);
        result.RequestsByWeek[2].Count.Should().Be(0);
        result.RequestsByWeek[3].Count.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithNoData_ShouldReturnAllZeros()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-14); // 2 weeks
        var to = DateTime.UtcNow;

        _storeMock.Setup(s => s.GetTrendsAsync(orgId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new OrgDashboardTrendsData(
                new List<(int, int)>(),
                new List<(int, int)>()));

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardTrendsQuery(orgId, from, to));

        // Assert
        result.AdoptionsByWeek.Should().AllSatisfy(p => p.Count.Should().Be(0));
        result.RequestsByWeek.Should().AllSatisfy(p => p.Count.Should().Be(0));
    }

    [Fact]
    public async Task Handle_ShouldFormatLabelsCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var from = new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc);

        _storeMock.Setup(s => s.GetTrendsAsync(orgId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new OrgDashboardTrendsData(
                new List<(int, int)>(),
                new List<(int, int)>()));

        var handler = new GetOrgDashboardTrendsQueryHandler(_storeMock.Object);

        // Act
        var result = await handler.Handle(new GetOrgDashboardTrendsQuery(orgId, from, to));

        // Assert
        result.AdoptionsByWeek[0].Label.Should().Be("Apr 7");
        result.AdoptionsByWeek[0].WeekStart.Should().Be(from);
    }
}
