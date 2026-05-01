using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationsQuery(int Skip = 0, int Take = 20) : IQuery<GetOrganizationsResponse>;
public record OrganizationListItem(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
public record GetOrganizationsResponse(List<OrganizationListItem> Organizations, long Total, int Skip, int Take);

public class GetOrganizationsQueryHandler : IQueryHandler<GetOrganizationsQuery, GetOrganizationsResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public GetOrganizationsQueryHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<GetOrganizationsResponse> HandleAsync(GetOrganizationsQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _orgRepo.GetAllAsync(query.Skip, query.Take);
        var list = items.Select(o => new OrganizationListItem(o.Id, o.Name, o.Slug, o.Status == OrganizationStatus.Active, o.CreatedAt)).ToList();
        return new GetOrganizationsResponse(list, total, query.Skip, query.Take);
    }
}
