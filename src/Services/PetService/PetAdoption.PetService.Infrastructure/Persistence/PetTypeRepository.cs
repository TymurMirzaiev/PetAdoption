using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetTypeRepository : IPetTypeRepository
{
    private readonly PetServiceDbContext _db;

    public PetTypeRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PetType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PetTypes
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PetType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PetTypes
            .OrderBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PetType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PetTypes.FindAsync([id], cancellationToken);
    }

    public async Task<PetType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _db.PetTypes
            .FirstOrDefaultAsync(pt => pt.Code == normalizedCode, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _db.PetTypes.AnyAsync(pt => pt.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(PetType petType, CancellationToken cancellationToken = default)
    {
        _db.PetTypes.Add(petType);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PetType petType, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
