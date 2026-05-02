namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;

public record GetFavoritesResponse(IEnumerable<FavoriteWithPetDto> Items, long TotalCount, int Page, int PageSize);

public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, GetFavoritesResponse>
{
    private readonly IFavoriteQueryStore _queryStore;

    public GetFavoritesQueryHandler(IFavoriteQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetFavoritesResponse> Handle(GetFavoritesQuery request, CancellationToken cancellationToken = default)
    {
        if (request.Take <= 0)
            throw new ArgumentException("Take must be greater than zero.", nameof(request));

        var (items, total) = await _queryStore.GetByUserAsync(
            request.UserId, request.Skip, request.Take,
            request.PetTypeId, request.PetStatus, request.SortBy);
        return new GetFavoritesResponse(items, total, request.Skip / request.Take + 1, request.Take);
    }
}
