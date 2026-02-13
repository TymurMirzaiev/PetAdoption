using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdatePetTypeCommand(Guid Id, string Name) : IRequest<UpdatePetTypeResponse>;

public record UpdatePetTypeResponse(bool Success);

public class UpdatePetTypeCommandHandler : IRequestHandler<UpdatePetTypeCommand, UpdatePetTypeResponse>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public UpdatePetTypeCommandHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<UpdatePetTypeResponse> Handle(UpdatePetTypeCommand request, CancellationToken ct)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.Id, ct);
        if (petType == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetTypeNotFound,
                $"Pet type with ID '{request.Id}' was not found.");
        }

        petType.UpdateName(request.Name);

        await _petTypeRepository.UpdateAsync(petType, ct);

        return new UpdatePetTypeResponse(true);
    }
}
