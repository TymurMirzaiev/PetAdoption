namespace PetAdoption.PetService.Application.Queries;

public record OrgDashboardData(
    int TotalPets,
    int AvailablePets,
    int ReservedPets,
    int AdoptedPets,
    int TotalRequests,
    int PendingRequests,
    long TotalImpressions,
    long TotalSwipes);

public record OrgDashboardTrendsData(
    IReadOnlyList<(int WeekOffset, int Count)> AdoptionsByWeek,
    IReadOnlyList<(int WeekOffset, int Count)> RequestsByWeek);

public interface IOrgDashboardQueryStore
{
    Task<OrgDashboardData> GetDashboardAsync(Guid orgId, CancellationToken ct);
    Task<OrgDashboardTrendsData> GetTrendsAsync(Guid orgId, DateTime from, DateTime to, CancellationToken ct);
}
