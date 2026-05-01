using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetCommand(
    string Name,
    Guid PetTypeId,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<CreatePetResponse>;
