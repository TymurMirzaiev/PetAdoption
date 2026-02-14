using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreatePetCommand(string Name, Guid PetTypeId) : IRequest<CreatePetResponse>;
