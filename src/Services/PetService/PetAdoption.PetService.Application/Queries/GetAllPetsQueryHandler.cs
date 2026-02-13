using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public class GetAllPetsQuery : IRequest<IEnumerable<PetListItemDto>>
{
}

public class GetAllPetsQueryHandler : IRequestHandler<GetAllPetsQuery, IEnumerable<PetListItemDto>>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetAllPetsQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<IEnumerable<PetListItemDto>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken = default)
    {
        var pets = await _queryStore.GetAll();
        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);

        // Create a dictionary for efficient lookup
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        return pets.Select(p => new PetListItemDto(
            p.Id,
            p.Name,
            petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown"),
            p.Status.ToString()
        ));
    }
}
