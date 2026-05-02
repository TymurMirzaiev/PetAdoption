using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetMyOrganizationsQuery(string UserId) : IQuery<GetMyOrganizationsResponse>;
public record MyOrganizationItem(Guid OrganizationId, string OrganizationName, string Slug, string Role, DateTime JoinedAt);
public record GetMyOrganizationsResponse(List<MyOrganizationItem> Organizations);

public class GetMyOrganizationsQueryHandler : IQueryHandler<GetMyOrganizationsQuery, GetMyOrganizationsResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    private readonly IOrganizationRepository _orgRepo;

    public GetMyOrganizationsQueryHandler(IOrganizationMemberRepository memberRepo, IOrganizationRepository orgRepo)
    {
        _memberRepo = memberRepo;
        _orgRepo = orgRepo;
    }

    public async Task<GetMyOrganizationsResponse> HandleAsync(GetMyOrganizationsQuery query, CancellationToken cancellationToken = default)
    {
        var memberships = await _memberRepo.GetByUserAsync(query.UserId);
        var membershipList = memberships.ToList();

        var orgIds = membershipList.Select(m => m.OrganizationId);
        var orgs = (await _orgRepo.GetByIdsAsync(orgIds)).ToDictionary(o => o.Id);

        var items = membershipList
            .Where(m => orgs.ContainsKey(m.OrganizationId))
            .Select(m =>
            {
                var org = orgs[m.OrganizationId];
                return new MyOrganizationItem(org.Id, org.Name, org.Slug, m.Role.ToString(), m.JoinedAt);
            })
            .ToList();

        return new GetMyOrganizationsResponse(items);
    }
}
