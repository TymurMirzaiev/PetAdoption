using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAdoptionRequestCommand(
    Guid UserId,
    Guid PetId,
    string? Message) : IRequest<CreateAdoptionRequestResponse>;
