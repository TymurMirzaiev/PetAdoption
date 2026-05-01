namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

public record CheckFavoriteQuery(Guid UserId, Guid PetId) : IRequest<CheckFavoriteResponse>;

public record CheckFavoriteResponse(bool IsFavorited);

public class CheckFavoriteQueryHandler : IRequestHandler<CheckFavoriteQuery, CheckFavoriteResponse>
{
    private readonly IFavoriteRepository _repository;

    public CheckFavoriteQueryHandler(IFavoriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckFavoriteResponse> Handle(CheckFavoriteQuery request, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsByUserAndPetAsync(request.UserId, request.PetId, ct);
        return new CheckFavoriteResponse(exists);
    }
}
