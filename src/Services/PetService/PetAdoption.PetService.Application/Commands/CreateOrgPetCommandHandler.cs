using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreateOrgPetResponse(Guid Id);

public class CreateOrgPetCommandHandler : IRequestHandler<CreateOrgPetCommand, CreateOrgPetResponse>
{
    private readonly IPetRepository _petRepository;

    public CreateOrgPetCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<CreateOrgPetResponse> Handle(CreateOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = Pet.Create(
            request.Name,
            request.PetTypeId,
            request.Breed,
            request.AgeMonths,
            request.Description,
            request.Tags);

        pet.AssignToOrganization(request.OrganizationId);

        await _petRepository.Add(pet);
        return new CreateOrgPetResponse(pet.Id);
    }
}
