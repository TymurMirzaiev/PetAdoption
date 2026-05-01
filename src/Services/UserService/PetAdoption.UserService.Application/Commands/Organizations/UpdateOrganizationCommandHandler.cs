using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class UpdateOrganizationCommandHandler : ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;

    public UpdateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<UpdateOrganizationResponse> HandleAsync(UpdateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null)
            return new UpdateOrganizationResponse(false, "Organization not found");

        org.Update(command.Name, command.Description);
        await _orgRepo.UpdateAsync(org);
        return new UpdateOrganizationResponse(true, "Organization updated");
    }
}
