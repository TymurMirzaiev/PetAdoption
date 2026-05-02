using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetMediaQuery(Guid PetId) : IRequest<GetPetMediaResponse>;

public record PetMediaItemDto(
    Guid Id,
    string MediaType,
    string Url,
    string ContentType,
    int SortOrder,
    bool IsPrimary,
    DateTime CreatedAt);

public record GetPetMediaResponse(IReadOnlyList<PetMediaItemDto> Items);

public class GetPetMediaQueryHandler : IRequestHandler<GetPetMediaQuery, GetPetMediaResponse>
{
    private readonly IPetQueryStore _petQueryStore;

    public GetPetMediaQueryHandler(IPetQueryStore petQueryStore)
    {
        _petQueryStore = petQueryStore;
    }

    public async Task<GetPetMediaResponse> Handle(GetPetMediaQuery request, CancellationToken ct = default)
    {
        var pet = await _petQueryStore.GetById(request.PetId);
        if (pet is null)
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });

        var items = pet.Media
            .OrderBy(m => m.IsPrimary ? 0 : 1)
            .ThenBy(m => m.SortOrder)
            .Select(m => new PetMediaItemDto(
                m.Id,
                m.MediaType.ToString(),
                m.Url,
                m.ContentType,
                m.SortOrder,
                m.IsPrimary,
                m.CreatedAt))
            .ToList();

        return new GetPetMediaResponse(items);
    }
}
