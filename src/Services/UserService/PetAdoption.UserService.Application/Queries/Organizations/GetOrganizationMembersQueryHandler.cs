using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationMembersQuery(Guid OrganizationId) : IQuery<GetOrganizationMembersResponse>;
public record OrganizationMemberItem(Guid Id, Guid OrganizationId, string UserId, string Role, DateTime JoinedAt);
public record GetOrganizationMembersResponse(List<OrganizationMemberItem> Members);

public class GetOrganizationMembersQueryHandler : IQueryHandler<GetOrganizationMembersQuery, GetOrganizationMembersResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    public GetOrganizationMembersQueryHandler(IOrganizationMemberRepository memberRepo) => _memberRepo = memberRepo;

    public async Task<GetOrganizationMembersResponse> HandleAsync(GetOrganizationMembersQuery query, CancellationToken cancellationToken = default)
    {
        var members = await _memberRepo.GetByOrganizationAsync(query.OrganizationId);
        var items = members.Select(m => new OrganizationMemberItem(m.Id, m.OrganizationId, m.UserId, m.Role.ToString(), m.JoinedAt)).ToList();
        return new GetOrganizationMembersResponse(items);
    }
}
