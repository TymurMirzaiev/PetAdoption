using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgAdoptionRequestsQuery(
    Guid OrganizationId,
    AdoptionRequestStatus? Status = null,
    int Skip = 0,
    int Take = 20) : IRequest<GetOrgAdoptionRequestsResponse>;

public record OrgAdoptionRequestDto(
    Guid Id,
    Guid UserId,
    Guid PetId,
    string PetName,
    string Status,
    string? Message,
    DateTime CreatedAt);

public record GetOrgAdoptionRequestsResponse(
    List<OrgAdoptionRequestDto> Items,
    long Total,
    int Skip,
    int Take);

public class GetOrgAdoptionRequestsQueryHandler
    : IRequestHandler<GetOrgAdoptionRequestsQuery, GetOrgAdoptionRequestsResponse>
{
    private readonly IAdoptionRequestQueryStore _queryStore;
    private readonly IPetQueryStore _petQueryStore;

    public GetOrgAdoptionRequestsQueryHandler(
        IAdoptionRequestQueryStore queryStore,
        IPetQueryStore petQueryStore)
    {
        _queryStore = queryStore;
        _petQueryStore = petQueryStore;
    }

    public async Task<GetOrgAdoptionRequestsResponse> Handle(
        GetOrgAdoptionRequestsQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetByOrganizationAsync(
            request.OrganizationId, request.Status, request.Skip, request.Take);

        var itemsList = items.ToList();
        var petIds = itemsList.Select(i => i.PetId).Distinct();
        var pets = await _petQueryStore.GetByIds(petIds);
        var petDict = pets.ToDictionary(p => p.Id);

        var dtos = itemsList.Select(item =>
        {
            petDict.TryGetValue(item.PetId, out var pet);
            return new OrgAdoptionRequestDto(
                item.Id,
                item.UserId,
                item.PetId,
                pet?.Name?.Value ?? "Unknown",
                item.Status.ToString(),
                item.Message,
                item.CreatedAt);
        }).ToList();

        return new GetOrgAdoptionRequestsResponse(dtos, total, request.Skip, request.Take);
    }
}
