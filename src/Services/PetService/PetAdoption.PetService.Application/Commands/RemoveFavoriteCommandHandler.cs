namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record RemoveFavoriteResponse(bool Success);

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, RemoveFavoriteResponse>
{
    private readonly IFavoriteRepository _favoriteRepository;

    public RemoveFavoriteCommandHandler(IFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task<RemoveFavoriteResponse> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken = default)
    {
        var existing = await _favoriteRepository.GetByUserAndPetAsync(request.UserId, request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.FavoriteNotFound, "Favorite not found.");

        await _favoriteRepository.DeleteAsync(request.UserId, request.PetId);
        return new RemoveFavoriteResponse(true);
    }
}
