using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public class CreateOrganizationCommandHandler : ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse>
{
    private readonly IOrganizationRepository _orgRepo;

    public CreateOrganizationCommandHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<CreateOrganizationResponse> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _orgRepo.GetBySlugAsync(command.Slug);
        if (existing is not null)
            return new CreateOrganizationResponse(false, Guid.Empty, "Organization with this slug already exists");

        var org = Organization.Create(command.Name, command.Slug, command.Description);
        await _orgRepo.AddAsync(org);
        return new CreateOrganizationResponse(true, org.Id, "Organization created successfully");
    }
}
