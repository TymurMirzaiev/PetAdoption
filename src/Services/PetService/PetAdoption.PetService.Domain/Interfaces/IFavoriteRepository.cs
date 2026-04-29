namespace PetAdoption.PetService.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task AddAsync(Favorite favorite);
    Task<Favorite?> GetByUserAndPetAsync(Guid userId, Guid petId);
    Task DeleteAsync(Guid userId, Guid petId);
}
