using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdateOrgPetCommand(
    Guid OrganizationId,
    Guid PetId,
    string Name,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<UpdateOrgPetResponse>;

public record UpdateOrgPetResponse(
    Guid Id, string Name, string Status, string? Breed, int? AgeMonths, string? Description, List<string> Tags);

public class UpdateOrgPetCommandHandler : IRequestHandler<UpdateOrgPetCommand, UpdateOrgPetResponse>
{
    private readonly IPetRepository _repository;

    public UpdateOrgPetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateOrgPetResponse> Handle(UpdateOrgPetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetByIdOrThrowAsync(request.PetId);

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

        pet.UpdateName(request.Name);
        pet.UpdateBreed(request.Breed);
        pet.UpdateAge(request.AgeMonths);
        pet.UpdateDescription(request.Description);

        if (request.Tags is not null)
            pet.SetTags(request.Tags);

        await _repository.Update(pet);

        return new UpdateOrgPetResponse(
            pet.Id, pet.Name, pet.Status.ToString(), pet.Breed?.Value, pet.Age?.Months, pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList());
    }
}
