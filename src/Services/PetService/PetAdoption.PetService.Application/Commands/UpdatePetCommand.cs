using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdatePetCommand(
    Guid PetId,
    string Name,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<UpdatePetResponse>;

public record UpdatePetResponse(Guid Id, string Name, string Status, string? Breed, int? AgeMonths, string? Description, IReadOnlyList<string> Tags);

public class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand, UpdatePetResponse>
{
    private readonly IPetRepository _repository;

    public UpdatePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdatePetResponse> Handle(UpdatePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetByIdOrThrowAsync(request.PetId);

        pet.UpdateName(request.Name);
        pet.UpdateBreed(request.Breed);
        pet.UpdateAge(request.AgeMonths);
        pet.UpdateDescription(request.Description);

        if (request.Tags is not null)
            pet.SetTags(request.Tags);

        await _repository.Update(pet);

        return new UpdatePetResponse(
            pet.Id, pet.Name, pet.Status.ToString(), pet.Breed?.Value, pet.Age?.Months, pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList());
    }
}
