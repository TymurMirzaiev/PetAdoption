using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetDiscoverPetsQuery(
    Guid UserId,
    Guid? PetTypeId,
    int? MinAgeMonths,
    int? MaxAgeMonths,
    int Take = 10) : IRequest<GetDiscoverPetsResponse>;

public record GetDiscoverPetsResponse(
    List<PetListItemDto> Pets,
    bool HasMore);

public class GetDiscoverPetsQueryHandler : IRequestHandler<GetDiscoverPetsQuery, GetDiscoverPetsResponse>
{
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetSkipRepository _skipRepository;
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetDiscoverPetsQueryHandler(
        IPetQueryStore petQueryStore,
        IPetSkipRepository skipRepository,
        IFavoriteRepository favoriteRepository,
        IPetTypeRepository petTypeRepository)
    {
        _petQueryStore = petQueryStore;
        _skipRepository = skipRepository;
        _favoriteRepository = favoriteRepository;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetDiscoverPetsResponse> Handle(GetDiscoverPetsQuery request, CancellationToken ct)
    {
        // 1. Get user's skipped pet IDs
        var skippedPetIds = await _skipRepository.GetPetIdsByUserAsync(request.UserId);

        // 2. Get user's favorited pet IDs
        var favoritedPetIds = await _favoriteRepository.GetPetIdsByUserAsync(request.UserId);

        // 3. Combine exclusion set
        var excludedIds = skippedPetIds.Concat(favoritedPetIds).ToHashSet();

        // 4. Query via query store, fetch one extra to determine HasMore
        var (pets, _) = await _petQueryStore.GetDiscoverable(
            excludedIds,
            request.PetTypeId,
            request.MinAgeMonths,
            request.MaxAgeMonths,
            request.Take + 1);

        var petList = pets.ToList();
        var hasMore = petList.Count > request.Take;
        if (hasMore)
            petList = petList.Take(request.Take).ToList();

        // 5. Map to DTOs
        var petTypes = await _petTypeRepository.GetAllAsync(ct);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = petList.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString(),
            p.Breed?.Value,
            p.Age?.Months,
            p.Description?.Value,
            p.Tags.Select(t => t.Value).ToList()
        )).ToList();

        return new GetDiscoverPetsResponse(items, hasMore);
    }
}
