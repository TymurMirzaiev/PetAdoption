using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetByIdQuery : IRequest<PetDetailsDto>
{
    public GetPetByIdQuery(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public record PetDetailsDto(
    Guid Id,
    string Name,
    string Type,
    string Status
);

public class GetPetByIdQueryHandler : IRequestHandler<GetPetByIdQuery, PetDetailsDto>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetPetByIdQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<PetDetailsDto> Handle(GetPetByIdQuery request, CancellationToken cancellationToken = default)
    {
        var pet = await _queryStore.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        // Fetch pet type
        var petType = await _petTypeRepository.GetByIdAsync(pet.PetTypeId, cancellationToken);
        var petTypeName = petType?.Name ?? "Unknown";

        return new PetDetailsDto(
            pet.Id,
            pet.Name,
            petTypeName,
            pet.Status.ToString()
        );
    }
}
