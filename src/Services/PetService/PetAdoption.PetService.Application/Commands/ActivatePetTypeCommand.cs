using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record ActivatePetTypeCommand(Guid Id) : IRequest<ActivatePetTypeResponse>;

public record ActivatePetTypeResponse(bool Success);

public class ActivatePetTypeCommandHandler : IRequestHandler<ActivatePetTypeCommand, ActivatePetTypeResponse>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public ActivatePetTypeCommandHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<ActivatePetTypeResponse> Handle(ActivatePetTypeCommand request, CancellationToken ct)
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

        return new ActivatePetTypeResponse(true);
    }
}
