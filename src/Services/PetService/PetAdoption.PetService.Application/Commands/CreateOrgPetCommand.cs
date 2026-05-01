using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreateOrgPetCommand(
    Guid OrganizationId,
    string Name,
    Guid PetTypeId,
    string? Breed = null,
    int? AgeMonths = null,
    string? Description = null,
    IEnumerable<string>? Tags = null) : IRequest<CreateOrgPetResponse>;
