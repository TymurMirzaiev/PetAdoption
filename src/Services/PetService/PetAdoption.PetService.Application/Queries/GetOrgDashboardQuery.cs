using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgDashboardQuery(Guid OrgId) : IRequest<GetOrgDashboardResponse>;

public record GetOrgDashboardResponse(
    int TotalPets,
    int AvailablePets,
    int ReservedPets,
    int AdoptedPets,
    int TotalAdoptionRequests,
    int PendingRequests,
    double AdoptionRate,
    long TotalImpressions,
    double AvgSwipeRate);

public class GetOrgDashboardQueryHandler
    : IRequestHandler<GetOrgDashboardQuery, GetOrgDashboardResponse>
{
    private readonly IOrgDashboardQueryStore _queryStore;

    public GetOrgDashboardQueryHandler(IOrgDashboardQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetOrgDashboardResponse> Handle(
        GetOrgDashboardQuery request, CancellationToken ct = default)
    {
        var data = await _queryStore.GetDashboardAsync(request.OrgId, ct);

        var adoptionRate = data.TotalPets == 0
            ? 0
            : Math.Round(data.AdoptedPets / (double)data.TotalPets * 100, 1);

        var avgSwipeRate = data.TotalImpressions == 0
            ? 0
            : Math.Round(data.TotalSwipes / (double)data.TotalImpressions * 100, 1);

        return new GetOrgDashboardResponse(
            data.TotalPets,
            data.AvailablePets,
            data.ReservedPets,
            data.AdoptedPets,
            data.TotalRequests,
            data.PendingRequests,
            adoptionRate,
            data.TotalImpressions,
            avgSwipeRate);
    }
}
