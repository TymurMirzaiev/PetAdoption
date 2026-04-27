using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeletePetCommand(Guid PetId) : IRequest<DeletePetResponse>;

public record DeletePetResponse(bool Success, string Message);

public class DeletePetCommandHandler : IRequestHandler<DeletePetCommand, DeletePetResponse>
{
    private readonly IPetRepository _repository;

    public DeletePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeletePetResponse> Handle(DeletePetCommand request, CancellationToken cancellationToken = default)
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

        pet.EnsureCanBeDeleted();
        await _repository.Delete(pet.Id);

        return new DeletePetResponse(true, $"Pet '{pet.Name}' has been deleted.");
    }
}
