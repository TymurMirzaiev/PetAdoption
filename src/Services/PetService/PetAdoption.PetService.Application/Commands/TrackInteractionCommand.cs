using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Commands;

public record TrackInteractionCommand(
    Guid PetId,
    Guid UserId,
    InteractionType Type) : IRequest<TrackInteractionResponse>;
