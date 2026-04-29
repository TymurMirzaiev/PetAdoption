using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetAnnouncementsResponse(IEnumerable<AnnouncementListDto> Items, long TotalCount, int Skip, int Take);

public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, GetAnnouncementsResponse>
{
    private readonly IAnnouncementQueryStore _queryStore;

    public GetAnnouncementsQueryHandler(IAnnouncementQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetAnnouncementsResponse> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetAllAsync(request.Skip, request.Take);
        return new GetAnnouncementsResponse(items, total, request.Skip, request.Take);
    }
}
