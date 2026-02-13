using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeactivatePetTypeCommand(Guid Id) : IRequest<DeactivatePetTypeResponse>;

public record DeactivatePetTypeResponse(bool Success);

public class DeactivatePetTypeCommandHandler : IRequestHandler<DeactivatePetTypeCommand, DeactivatePetTypeResponse>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public DeactivatePetTypeCommandHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<DeactivatePetTypeResponse> Handle(DeactivatePetTypeCommand request, CancellationToken ct)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.Id, ct);
        if (petType == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetTypeNotFound,
                $"Pet type with ID '{request.Id}' was not found.");
        }

        petType.Deactivate();

        await _petTypeRepository.UpdateAsync(petType, ct);

        return new DeactivatePetTypeResponse(true);
    }
}
