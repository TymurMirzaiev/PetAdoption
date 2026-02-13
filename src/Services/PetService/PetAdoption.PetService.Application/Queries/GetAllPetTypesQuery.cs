using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetAllPetTypesQuery(bool IncludeInactive = false) : IRequest<IEnumerable<PetTypeDto>>;

public class GetAllPetTypesQueryHandler : IRequestHandler<GetAllPetTypesQuery, IEnumerable<PetTypeDto>>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public GetAllPetTypesQueryHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<IEnumerable<PetTypeDto>> Handle(GetAllPetTypesQuery request, CancellationToken ct)
    {
        var petTypes = request.IncludeInactive
            ? await _petTypeRepository.GetAllAsync(ct)
            : await _petTypeRepository.GetAllActiveAsync(ct);

        return petTypes.Select(pt => new PetTypeDto
        {
            Id = pt.Id,
            Code = pt.Code,
            Name = pt.Name,
            IsActive = pt.IsActive,
            CreatedAt = pt.CreatedAt,
            UpdatedAt = pt.UpdatedAt
        });
    }
}
