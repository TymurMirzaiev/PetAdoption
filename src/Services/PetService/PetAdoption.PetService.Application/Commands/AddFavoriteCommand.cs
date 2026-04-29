namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record AddFavoriteCommand(Guid UserId, Guid PetId) : IRequest<AddFavoriteResponse>;
