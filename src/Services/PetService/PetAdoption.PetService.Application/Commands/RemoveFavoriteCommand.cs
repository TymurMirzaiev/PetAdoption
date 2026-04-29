namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record RemoveFavoriteCommand(Guid UserId, Guid PetId) : IRequest<RemoveFavoriteResponse>;
