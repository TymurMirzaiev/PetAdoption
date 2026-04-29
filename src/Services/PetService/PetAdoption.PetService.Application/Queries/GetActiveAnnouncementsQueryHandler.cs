using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public class GetActiveAnnouncementsQueryHandler : IRequestHandler<GetActiveAnnouncementsQuery, IEnumerable<ActiveAnnouncementDto>>
{
    private readonly IAnnouncementQueryStore _queryStore;

    public GetActiveAnnouncementsQueryHandler(IAnnouncementQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<IEnumerable<ActiveAnnouncementDto>> Handle(GetActiveAnnouncementsQuery request, CancellationToken cancellationToken = default)
    {
        return await _queryStore.GetActiveAsync();
    }
}
