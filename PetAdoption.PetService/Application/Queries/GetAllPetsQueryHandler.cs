using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Queries;

public class GetAllPetsQuery : IRequest<IEnumerable<PetListItemDto>>
{
}

public class GetAllPetsQueryHandler : IRequestHandler<GetAllPetsQuery, IEnumerable<PetListItemDto>>
{
    private readonly IPetRepository _repo;
    public GetAllPetsQueryHandler(IPetRepository repo) => _repo = repo;

    public async Task<IEnumerable<PetListItemDto>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken = default)
    {
        var pets = await _repo.GetAll();
        return pets.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            p.Type,
            p.Status.ToString()
        ));
    }
}
