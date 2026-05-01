using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetsQuery(
    PetStatus? Status,
    Guid? PetTypeId,
    int Skip = 0,
    int Take = 20,
    int? MinAgeMonths = null,
    int? MaxAgeMonths = null,
    string? BreedSearch = null) : IRequest<GetPetsResponse>;

public record GetPetsResponse(
    List<PetListItemDto> Pets,
    long Total,
    int Skip,
    int Take);

public class GetPetsQueryHandler : IRequestHandler<GetPetsQuery, GetPetsResponse>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetPetsQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetPetsResponse> Handle(GetPetsQuery request, CancellationToken cancellationToken = default)
    {
        var (pets, total) = await _queryStore.GetFiltered(
            request.Status,
            request.PetTypeId,
            request.Skip,
            request.Take,
            request.MinAgeMonths,
            request.MaxAgeMonths,
            request.BreedSearch);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = pets.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString(),
            p.Breed?.Value,
            p.Age?.Months,
            p.Description?.Value
        )).ToList();

        return new GetPetsResponse(items, total, request.Skip, request.Take);
    }
}
