using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Application.Abstractions;

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

    public GetPetByIdQueryHandler(IPetQueryStore queryStore)
    {
        _queryStore = queryStore;
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

        return new PetDetailsDto(
            pet.Id,
            pet.Name,
            pet.Type,
            pet.Status.ToString()
        );
    }
}
