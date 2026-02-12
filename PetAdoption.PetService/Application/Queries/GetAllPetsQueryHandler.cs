using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Queries;

public class GetAllPetsQuery : IRequest<IEnumerable<PetListItemDto>>
{
}

public class GetAllPetsQueryHandler : IRequestHandler<GetAllPetsQuery, IEnumerable<PetListItemDto>>
{
    private readonly IPetQueryStore _queryStore;
    public GetAllPetsQueryHandler(IPetQueryStore queryStore) => _queryStore = queryStore;

    public async Task<IEnumerable<PetListItemDto>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken = default)
    {
        var pets = await _queryStore.GetAll();
        return pets.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            p.Type,
            p.Status.ToString()
        ));
    }
}
