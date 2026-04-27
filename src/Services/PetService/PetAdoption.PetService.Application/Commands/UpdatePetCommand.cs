using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdatePetCommand(Guid PetId, string Name) : IRequest<UpdatePetResponse>;

public record UpdatePetResponse(Guid Id, string Name, string Status);

public class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand, UpdatePetResponse>
{
    private readonly IPetRepository _repository;

    public UpdatePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdatePetResponse> Handle(UpdatePetCommand request, CancellationToken cancellationToken = default)
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

        pet.UpdateName(request.Name);
        await _repository.Update(pet);

        return new UpdatePetResponse(pet.Id, pet.Name, pet.Status.ToString());
    }
}
