using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetQueryStore : IPetQueryStore
{
    private readonly PetServiceDbContext _db;

    public PetQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Pet>> GetAll()
    {
        return await _db.Pets.AsNoTracking().ToListAsync();
    }

    public async Task<Pet?> GetById(Guid id)
    {
        return await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
        PetStatus? status,
        Guid? petTypeId,
        int skip,
        int take)
    {
        var query = _db.Pets.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (petTypeId.HasValue)
            query = query.Where(p => p.PetTypeId == petTypeId.Value);

        var total = await query.LongCountAsync();
        var pets = await query.Skip(skip).Take(take).ToListAsync();

        return (pets, total);
    }
}
