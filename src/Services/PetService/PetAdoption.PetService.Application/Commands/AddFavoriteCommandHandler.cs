namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record AddFavoriteResponse(Guid Id, Guid PetId, DateTime CreatedAt);

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, AddFavoriteResponse>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IPetRepository _petRepository;

    public AddFavoriteCommandHandler(IFavoriteRepository favoriteRepository, IPetRepository petRepository)
    {
        _favoriteRepository = favoriteRepository;
        _petRepository = petRepository;
    }

    public async Task<AddFavoriteResponse> Handle(AddFavoriteCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");

        var existing = await _favoriteRepository.GetByUserAndPetAsync(request.UserId, request.PetId);
        if (existing is not null)
            throw new DomainException(PetDomainErrorCode.FavoriteAlreadyExists, "Pet is already in favorites.");

        var favorite = Favorite.Create(request.UserId, request.PetId);
        await _favoriteRepository.AddAsync(favorite);

        return new AddFavoriteResponse(favorite.Id, favorite.PetId, favorite.CreatedAt);
    }
}
