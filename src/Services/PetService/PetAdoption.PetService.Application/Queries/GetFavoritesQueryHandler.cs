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
        var (items, total) = await _queryStore.GetByUserAsync(request.UserId, request.Skip, request.Take);
        return new GetFavoritesResponse(items, total, request.Skip / request.Take + 1, request.Take);
    }
}
