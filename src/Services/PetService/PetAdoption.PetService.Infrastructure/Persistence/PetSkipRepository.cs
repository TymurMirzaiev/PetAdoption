using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetSkipRepository : IPetSkipRepository
{
    private readonly PetServiceDbContext _db;

    public PetSkipRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PetSkip skip)
    {
        _db.PetSkips.Add(skip);
        await _db.SaveChangesAsync();
    }

    public async Task<PetSkip?> GetByUserAndPetAsync(Guid userId, Guid petId)
    {
        return await _db.PetSkips
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PetId == petId);
    }

    public async Task<IReadOnlyList<Guid>> GetPetIdsByUserAsync(Guid userId)
    {
        return await _db.PetSkips
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.PetId)
            .ToListAsync();
    }

    public async Task DeleteAllByUserAsync(Guid userId)
    {
        await _db.PetSkips
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
