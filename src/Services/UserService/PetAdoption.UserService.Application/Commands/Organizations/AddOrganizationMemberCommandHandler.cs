using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class AddOrganizationMemberCommandHandler : ICommandHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IOrganizationMemberRepository _memberRepo;

    public AddOrganizationMemberCommandHandler(IOrganizationRepository orgRepo, IOrganizationMemberRepository memberRepo)
    {
        _orgRepo = orgRepo;
        _memberRepo = memberRepo;
    }

    public async Task<AddOrganizationMemberResponse> HandleAsync(AddOrganizationMemberCommand command, CancellationToken cancellationToken = default)
    {
        var org = await _orgRepo.GetByIdAsync(command.OrganizationId);
        if (org is null) return new AddOrganizationMemberResponse(false, "Organization not found");

        var existing = await _memberRepo.GetByOrgAndUserAsync(command.OrganizationId, command.UserId);
        if (existing is not null) return new AddOrganizationMemberResponse(false, "User is already a member");

        if (!Enum.TryParse<OrgRole>(command.Role, true, out var role))
            return new AddOrganizationMemberResponse(false, "Invalid role. Use 'Admin' or 'Moderator'");

        var member = OrganizationMember.Create(command.OrganizationId, command.UserId, role);
        await _memberRepo.AddAsync(member);
        return new AddOrganizationMemberResponse(true, "Member added");
    }
}
