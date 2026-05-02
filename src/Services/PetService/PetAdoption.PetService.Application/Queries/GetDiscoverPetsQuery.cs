using Microsoft.Extensions.Options;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Mappings;
using PetAdoption.PetService.Application.Options;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetDiscoverPetsQuery(
    Guid UserId,
    Guid? PetTypeId,
    int? MinAgeMonths,
    int? MaxAgeMonths,
    int Take = 10,
    string? BreedSearch = null,
    decimal? Lat = null,
    decimal? Lng = null,
    int? RadiusKm = null) : IRequest<GetDiscoverPetsResponse>;

public record GetDiscoverPetsResponse(
    List<PetListItemDto> Pets,
    bool HasMore);

public class GetDiscoverPetsQueryHandler : IRequestHandler<GetDiscoverPetsQuery, GetDiscoverPetsResponse>
{
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetSkipRepository _skipRepository;
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IPetTypeRepository _petTypeRepository;
    private readonly IOptions<DiscoverOptions> _discoverOptions;
    private readonly IPetRankingService _rankingService;

    public GetDiscoverPetsQueryHandler(
        IPetQueryStore petQueryStore,
        IPetSkipRepository skipRepository,
        IFavoriteRepository favoriteRepository,
        IPetTypeRepository petTypeRepository,
        IOptions<DiscoverOptions> discoverOptions,
        IPetRankingService rankingService)
    {
        _petQueryStore = petQueryStore;
        _skipRepository = skipRepository;
        _favoriteRepository = favoriteRepository;
        _petTypeRepository = petTypeRepository;
        _discoverOptions = discoverOptions;
        _rankingService = rankingService;
    }

    public async Task<GetDiscoverPetsResponse> Handle(GetDiscoverPetsQuery request, CancellationToken ct)
    {
        // Validate location parameters — all three must be provided or none
        var locationParamsProvided = new[] { request.Lat.HasValue, request.Lng.HasValue, request.RadiusKm.HasValue };
        var providedCount = locationParamsProvided.Count(x => x);
        if (providedCount > 0 && providedCount < 3)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidLocationFilter,
                "lat, lng, and radiusKm must all be provided together.");
        }

        // Clamp radiusKm to [1, 20000] (20000 km ≈ half Earth's circumference)
        int? radiusKm = request.RadiusKm.HasValue
            ? Math.Max(1, Math.Min(20000, request.RadiusKm.Value))
            : null;

        // 1+2. Fetch skipped and favorited pet IDs (sequential — same scoped DbContext)
        var skippedPetIds = await _skipRepository.GetPetIdsByUserAsync(request.UserId);
        var favoritedPetIds = await _favoriteRepository.GetPetIdsByUserAsync(request.UserId);

        // 3. Combine exclusion set
        var excludedIds = skippedPetIds.Concat(favoritedPetIds).ToHashSet();

        // 4. Determine pool size based on ranking flag
        var options = _discoverOptions.Value;
        var useRanking = options.RankingEnabled
            && await _rankingService.UserHasEnoughSignalsAsync(request.UserId, ct);

        int poolSize = useRanking
            ? Math.Min(request.Take * options.CandidatePoolMultiplier, options.CandidatePoolCap)
            : request.Take + 1;

        // 5. Query via query store
        var (pets, _) = await _petQueryStore.GetDiscoverable(
            excludedIds,
            request.PetTypeId,
            request.MinAgeMonths,
            request.MaxAgeMonths,
            poolSize,
            request.BreedSearch,
            request.Lat,
            request.Lng,
            radiusKm,
            poolSize);

        var petList = pets.ToList();

        // 6. Rank if ranking is active
        if (useRanking)
            petList = (await _rankingService.RankAsync(request.UserId, petList, ct)).ToList();

        var hasMore = petList.Count > request.Take;
        if (hasMore)
            petList = petList.Take(request.Take).ToList();

        // 7. Map to DTOs
        var petTypes = await _petTypeRepository.GetAllAsync(ct);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = petList
            .Select(p => PetMapper.ToPetListItemDto(p, petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown")))
            .ToList();

        return new GetDiscoverPetsResponse(items, hasMore);
    }
}
