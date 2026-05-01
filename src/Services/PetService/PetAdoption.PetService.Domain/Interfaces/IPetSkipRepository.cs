namespace PetAdoption.PetService.Domain.Interfaces;

public interface IPetSkipRepository
{
    Task AddAsync(PetSkip skip);
    Task<PetSkip?> GetByUserAndPetAsync(Guid userId, Guid petId);
    Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId);
    Task DeleteAllByUserAsync(Guid userId);
}
