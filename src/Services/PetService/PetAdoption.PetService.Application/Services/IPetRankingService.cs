using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Services;

public interface IPetRankingService
{
    Task<bool> UserHasEnoughSignalsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Pet>> RankAsync(Guid userId, IReadOnlyList<Pet> candidates, CancellationToken ct);
}
