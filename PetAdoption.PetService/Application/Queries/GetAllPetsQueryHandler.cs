using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Infrastructure;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Queries;

public class GetAllPetsQuery : IRequest<IEnumerable<Pet>>
{
}

public class GetAllPetsQueryHandler : IRequestHandler<GetAllPetsQuery, IEnumerable<Pet>>
{
    private readonly IPetRepository _repo;
    public GetAllPetsQueryHandler(IPetRepository repo) => _repo = repo;

    public Task<IEnumerable<Pet>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken = default)
    {
        return _repo.GetAll();
    }
}
