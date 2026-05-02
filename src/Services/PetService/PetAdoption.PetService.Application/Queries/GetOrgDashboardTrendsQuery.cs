using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgDashboardTrendsQuery(Guid OrgId, DateTime? From, DateTime? To)
    : IRequest<GetOrgDashboardTrendsResponse>;

public record TrendPoint(DateTime WeekStart, string Label, int Count);

public record GetOrgDashboardTrendsResponse(
    IReadOnlyList<TrendPoint> AdoptionsByWeek,
    IReadOnlyList<TrendPoint> RequestsByWeek);

public class GetOrgDashboardTrendsQueryHandler
    : IRequestHandler<GetOrgDashboardTrendsQuery, GetOrgDashboardTrendsResponse>
{
    private readonly IOrgDashboardQueryStore _queryStore;

    public GetOrgDashboardTrendsQueryHandler(IOrgDashboardQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetOrgDashboardTrendsResponse> Handle(
        GetOrgDashboardTrendsQuery request, CancellationToken ct = default)
    {
        var from = request.From ?? DateTime.UtcNow.AddDays(-84);
        var to = request.To ?? DateTime.UtcNow;

        if (from >= to)
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                "End date must be after start date.");

        if ((to - from).TotalDays > 364)
            from = to.AddDays(-364);

        var data = await _queryStore.GetTrendsAsync(request.OrgId, from, to, ct);

        var totalWeeks = (int)(to - from).TotalDays / 7;

        var adoptionDict = data.AdoptionsByWeek.ToDictionary(x => x.WeekOffset, x => x.Count);
        var requestDict = data.RequestsByWeek.ToDictionary(x => x.WeekOffset, x => x.Count);

        var adoptions = new List<TrendPoint>();
        var requests = new List<TrendPoint>();

        for (var n = 0; n <= totalWeeks; n++)
        {
            var weekStart = from.AddDays(n * 7);
            var label = weekStart.ToString("MMM d");

            adoptions.Add(new TrendPoint(weekStart, label, adoptionDict.GetValueOrDefault(n, 0)));
            requests.Add(new TrendPoint(weekStart, label, requestDict.GetValueOrDefault(n, 0)));
        }

        return new GetOrgDashboardTrendsResponse(adoptions, requests);
    }
}
