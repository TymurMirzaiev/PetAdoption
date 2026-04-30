using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly PetServiceDbContext _db;

    public FavoriteRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Favorite favorite)
    {
        _db.Favorites.Add(favorite);
        await _db.SaveChangesAsync();
    }

    public async Task<Favorite?> GetByUserAndPetAsync(Guid userId, Guid petId)
    {
        return await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PetId == petId);
    }

    public async Task DeleteAsync(Guid userId, Guid petId)
    {
        await _db.Favorites
            .Where(f => f.UserId == userId && f.PetId == petId)
            .ExecuteDeleteAsync();
    }
}
