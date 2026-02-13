using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetTypeByIdQuery(Guid Id) : IRequest<PetTypeDto>;

public class GetPetTypeByIdQueryHandler : IRequestHandler<GetPetTypeByIdQuery, PetTypeDto>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public GetPetTypeByIdQueryHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<PetTypeDto> Handle(GetPetTypeByIdQuery request, CancellationToken ct)
    {
        var petType = await _petTypeRepository.GetByIdAsync(request.Id, ct);
        if (petType == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetTypeNotFound,
                $"Pet type with ID '{request.Id}' was not found.");
        }

        return new PetTypeDto
        {
            Id = petType.Id,
            Code = petType.Code,
            Name = petType.Name,
            IsActive = petType.IsActive,
            CreatedAt = petType.CreatedAt,
            UpdatedAt = petType.UpdatedAt
        };
    }
}
