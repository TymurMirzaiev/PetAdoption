using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class FavoriteQueryStore : IFavoriteQueryStore
{
    private readonly PetServiceDbContext _db;

    public FavoriteQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take)
    {
        var query = _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId);

        var total = await query.LongCountAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Join(_db.Pets.AsNoTracking(), f => f.PetId, p => p.Id, (f, p) => new { f, p })
            .Join(_db.PetTypes.AsNoTracking(), x => x.p.PetTypeId, pt => pt.Id, (x, pt) => new FavoriteWithPetDto(
                x.f.Id,
                x.f.PetId,
                x.p.Name.Value,
                pt.Name,
                x.p.Breed != null ? x.p.Breed.Value : null,
                x.p.Age != null ? x.p.Age.Months : (int?)null,
                x.p.Status.ToString(),
                x.f.CreatedAt))
            .ToListAsync();

        return (items, total);
    }
}
