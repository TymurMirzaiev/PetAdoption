using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetResponse(Guid Id);

public class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, CreatePetResponse>
{
    private readonly IPetRepository _petRepository;

    public CreatePetCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<CreatePetResponse> Handle(CreatePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = Pet.Create(request.Name, request.PetTypeId);
        await _petRepository.Add(pet);
        return new CreatePetResponse(pet.Id);
    }
}
