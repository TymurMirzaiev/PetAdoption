using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record ActivateOrganizationCommand(Guid Id) : ICommand<ActivateOrganizationResponse>;
public record ActivateOrganizationResponse(bool Success, string Message);

public class ActivateOrganizationCommandHandler : ICommandHandler<ActivateOrganizationCommand, ActivateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;
    public ActivateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<ActivateOrganizationResponse> HandleAsync(ActivateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var org = await _orgRepo.GetByIdAsync(command.Id);
        if (org is null) throw new OrganizationNotFoundException(command.Id);
        org.Activate();
        await _orgRepo.UpdateAsync(org);
        return new ActivateOrganizationResponse(true, "Organization activated");
    }
}
