using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.ValueObjects;

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
        int take,
        int? minAgeMonths = null,
        int? maxAgeMonths = null,
        string? breedSearch = null)
    {
        var query = _db.Pets.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (petTypeId.HasValue)
            query = query.Where(p => p.PetTypeId == petTypeId.Value);

        if (minAgeMonths.HasValue)
        {
            var minAge = new PetAge(minAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age >= minAge);
        }

        if (maxAgeMonths.HasValue)
        {
            var maxAge = new PetAge(maxAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age <= maxAge);
        }

        if (!string.IsNullOrWhiteSpace(breedSearch))
        {
            var trimmed = breedSearch.Trim();
            query = query.Where(p => p.Breed != null && EF.Functions.Like((string)p.Breed, $"%{trimmed}%"));
        }

        var total = await query.LongCountAsync();
        var pets = await query.OrderBy(p => p.Name).Skip(skip).Take(take).ToListAsync();

        return (pets, total);
    }
}
