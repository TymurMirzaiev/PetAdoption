using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Mappings;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetOrgPetsQuery(
    Guid OrganizationId,
    PetStatus? Status = null,
    int Skip = 0,
    int Take = 20,
    IEnumerable<string>? Tags = null) : IRequest<GetOrgPetsResponse>;

public record GetOrgPetsResponse(
    List<PetListItemDto> Pets, long Total, int Skip, int Take);

public class GetOrgPetsQueryHandler : IRequestHandler<GetOrgPetsQuery, GetOrgPetsResponse>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetOrgPetsQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetOrgPetsResponse> Handle(GetOrgPetsQuery request, CancellationToken cancellationToken = default)
    {
        var (pets, total) = await _queryStore.GetFilteredByOrg(
            request.OrganizationId, request.Status, request.Skip, request.Take, request.Tags);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var items = pets
            .Select(p => PetMapper.ToPetListItemDto(p, petTypeDict.GetValueOrDefault(p.PetTypeId, "Unknown")))
            .ToList();

        return new GetOrgPetsResponse(items, total, request.Skip, request.Take);
    }
}
