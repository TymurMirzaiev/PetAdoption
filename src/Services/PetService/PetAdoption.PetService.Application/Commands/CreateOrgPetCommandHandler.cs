using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreateOrgPetResponse(Guid Id);

public class CreateOrgPetCommandHandler : IRequestHandler<CreateOrgPetCommand, CreateOrgPetResponse>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetTypeRepository _petTypeRepository;

    public CreateOrgPetCommandHandler(IPetRepository petRepository, IPetTypeRepository petTypeRepository)
    {
        _petRepository = petRepository;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<CreateOrgPetResponse> Handle(CreateOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.PetTypeId, cancellationToken);
        if (petType is null)
            throw new DomainException(PetDomainErrorCode.PetTypeNotFound, "Pet type not found.");

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
