using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetTypeCommand(string Code, string Name) : IRequest<CreatePetTypeResponse>;

public record CreatePetTypeResponse(Guid Id, string Code, string Name);

public class CreatePetTypeCommandHandler : IRequestHandler<CreatePetTypeCommand, CreatePetTypeResponse>
{
    private readonly IPetTypeRepository _petTypeRepository;

    public CreatePetTypeCommandHandler(IPetTypeRepository petTypeRepository)
    {
        _petTypeRepository = petTypeRepository;
    }

    public async Task<CreatePetTypeResponse> Handle(CreatePetTypeCommand request, CancellationToken ct)
    {
        // Check if pet type already exists
        var exists = await _petTypeRepository.ExistsByCodeAsync(request.Code, ct);
        if (exists)
        {
            throw new DomainException(
                PetDomainErrorCode.PetTypeAlreadyExists,
                $"Pet type with code '{request.Code}' already exists.",
                new Dictionary<string, object>
                {
                    { "Code", request.Code }
                });
        }

        // Create new pet type
        var petType = PetType.Create(request.Code, request.Name);

        await _petTypeRepository.AddAsync(petType, ct);

        return new CreatePetTypeResponse(petType.Id, petType.Code, petType.Name);
    }
}
