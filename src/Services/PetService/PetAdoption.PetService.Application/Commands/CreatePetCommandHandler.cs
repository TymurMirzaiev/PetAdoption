using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetResponse(Guid Id);

public class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, CreatePetResponse>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetTypeRepository _petTypeRepository;

    public CreatePetCommandHandler(IPetRepository petRepository, IPetTypeRepository petTypeRepository)
    {
        _petRepository = petRepository;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<CreatePetResponse> Handle(CreatePetCommand request, CancellationToken cancellationToken = default)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.PetTypeId, cancellationToken);
        if (petType is null)
            throw new DomainException(PetDomainErrorCode.PetTypeNotFound, "Pet type not found.");

        var pet = Pet.Create(request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags);
        await _petRepository.Add(pet);
        return new CreatePetResponse(pet.Id);
    }
}
