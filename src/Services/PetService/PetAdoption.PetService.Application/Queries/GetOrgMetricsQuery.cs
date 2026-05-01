using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgMetricsQuery(
    Guid OrgId,
    DateTime? From,
    DateTime? To,
    string? SortBy,
    bool Descending = true) : IRequest<GetOrgMetricsResponse>;

public record GetOrgMetricsResponse(IEnumerable<PetMetricsSummary> Metrics);

public class GetOrgMetricsQueryHandler : IRequestHandler<GetOrgMetricsQuery, GetOrgMetricsResponse>
{
    private readonly IPetMetricsQueryStore _queryStore;

    public GetOrgMetricsQueryHandler(IPetMetricsQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetOrgMetricsResponse> Handle(GetOrgMetricsQuery request, CancellationToken ct = default)
    {
        var metrics = await _queryStore.GetMetricsByOrgAsync(
            request.OrgId, request.From, request.To, request.SortBy, request.Descending);
        return new GetOrgMetricsResponse(metrics);
    }
}
