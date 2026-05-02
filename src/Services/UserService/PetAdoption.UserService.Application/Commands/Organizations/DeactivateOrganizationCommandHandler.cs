using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record DeactivateOrganizationCommand(Guid Id) : ICommand<DeactivateOrganizationResponse>;
public record DeactivateOrganizationResponse(bool Success, string Message);

public class DeactivateOrganizationCommandHandler : ICommandHandler<DeactivateOrganizationCommand, DeactivateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public DeactivateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<DeactivateOrganizationResponse> HandleAsync(DeactivateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null) throw new OrganizationNotFoundException(command.Id);
        org.Deactivate();
        await _orgRepo.UpdateAsync(org);
        return new DeactivateOrganizationResponse(true, "Organization deactivated");
    }
}
