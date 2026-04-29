using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Queries;

public class GetAnnouncementByIdQueryHandler : IRequestHandler<GetAnnouncementByIdQuery, AnnouncementDetailDto>
{
    private readonly IAnnouncementQueryStore _queryStore;

    public GetAnnouncementByIdQueryHandler(IAnnouncementQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<AnnouncementDetailDto> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken = default)
    {
        return await _queryStore.GetByIdAsync(request.Id)
            ?? throw new DomainException(PetDomainErrorCode.AnnouncementNotFound, $"Announcement {request.Id} not found.");
    }
}
