using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record AdoptPetCommand : IRequest<AdoptPetResponse>
{
    public AdoptPetCommand(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public record AdoptPetResponse(
    bool Success,
    string? Message = null,
    Guid? PetId = null,
    string? Status = null
);

public class AdoptPetCommandHandler : IRequestHandler<AdoptPetCommand, AdoptPetResponse>
{
    private readonly IPetRepository _repository;

    public AdoptPetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdoptPetResponse> Handle(AdoptPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        pet.Adopt();
        await _repository.Update(pet);

        return new AdoptPetResponse(
            Success: true,
            PetId: pet.Id,
            Status: pet.Status.ToString()
        );
    }
}
