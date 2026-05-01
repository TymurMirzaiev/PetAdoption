using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeleteOrgPetCommand(Guid OrganizationId, Guid PetId) : IRequest<DeleteOrgPetResponse>;

public record DeleteOrgPetResponse(bool Success, string Message);

public class DeleteOrgPetCommandHandler : IRequestHandler<DeleteOrgPetCommand, DeleteOrgPetResponse>
{
    private readonly IPetRepository _repository;

    public DeleteOrgPetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteOrgPetResponse> Handle(DeleteOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });
        }

        if (pet.OrganizationId != request.OrganizationId)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found in organization {request.OrganizationId}.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId },
                    { "OrganizationId", request.OrganizationId }
                });
        }

        pet.EnsureCanBeDeleted();
        await _repository.Delete(pet.Id);

        return new DeleteOrgPetResponse(true, $"Pet '{pet.Name}' has been deleted.");
    }
}
