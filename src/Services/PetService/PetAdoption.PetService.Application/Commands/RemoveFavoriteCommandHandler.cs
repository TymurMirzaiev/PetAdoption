namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, Unit>
{
    private readonly IFavoriteRepository _favoriteRepository;

    public RemoveFavoriteCommandHandler(IFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task<Unit> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken = default)
    {
        var existing = await _favoriteRepository.GetByUserAndPetAsync(request.UserId, request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.FavoriteNotFound, "Favorite not found.");

        await _favoriteRepository.DeleteAsync(request.UserId, request.PetId);
        return Unit.Value;
    }
}
