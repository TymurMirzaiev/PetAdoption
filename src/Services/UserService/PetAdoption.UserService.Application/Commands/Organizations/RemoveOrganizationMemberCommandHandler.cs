using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record RemoveOrganizationMemberCommand(Guid OrganizationId, string UserId) : ICommand<RemoveOrganizationMemberResponse>;
public record RemoveOrganizationMemberResponse(bool Success, string Message);

public class RemoveOrganizationMemberCommandHandler : ICommandHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResponse>
{
    private readonly IOrganizationMemberRepository _memberRepo;
    public RemoveOrganizationMemberCommandHandler(IOrganizationMemberRepository memberRepo) => _memberRepo = memberRepo;

    public async Task<RemoveOrganizationMemberResponse> HandleAsync(RemoveOrganizationMemberCommand command, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepo.GetByOrgAndUserAsync(command.OrganizationId, command.UserId);
        if (member is null) return new RemoveOrganizationMemberResponse(false, "Member not found");
        await _memberRepo.DeleteAsync(member);
        return new RemoveOrganizationMemberResponse(true, "Member removed");
    }
}
