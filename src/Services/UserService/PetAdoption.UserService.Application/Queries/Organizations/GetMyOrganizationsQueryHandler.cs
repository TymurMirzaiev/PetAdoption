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
        var items = new List<MyOrganizationItem>();
        foreach (var m in memberships)
        {
            var org = await _orgRepo.GetByIdAsync(m.OrganizationId);
            if (org is not null)
                items.Add(new MyOrganizationItem(org.Id, org.Name, org.Slug, m.Role.ToString(), m.JoinedAt));
        }
        return new GetMyOrganizationsResponse(items);
    }
}
