using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record ActivatePetTypeCommand(Guid Id) : IRequest<Unit>;

public class ActivatePetTypeCommandHandler : IRequestHandler<ActivatePetTypeCommand, Unit>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public ActivatePetTypeCommandHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<Unit> Handle(ActivatePetTypeCommand request, CancellationToken ct)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.Id, ct);
        if (petType == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetTypeNotFound,
                $"Pet type with ID '{request.Id}' was not found.");
        }

        petType.Activate();

        await _petTypeRepository.UpdateAsync(petType, ct);

        return Unit.Value;
    }
}
