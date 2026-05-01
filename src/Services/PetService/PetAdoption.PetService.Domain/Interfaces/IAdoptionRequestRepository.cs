namespace PetAdoption.PetService.Domain.Interfaces;

public interface IAdoptionRequestRepository
{
    Task<AdoptionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdoptionRequest?> GetPendingByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default);
    Task AddAsync(AdoptionRequest request, CancellationToken ct = default);
    Task UpdateAsync(AdoptionRequest request, CancellationToken ct = default);
}
