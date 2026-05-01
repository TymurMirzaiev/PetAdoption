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

    public async Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(
        Guid userId, int skip, int take,
        Guid? petTypeId = null, string? petStatus = null, string sortBy = "newest")
    {
        var query = from f in _db.Favorites.AsNoTracking()
                    join p in _db.Pets.AsNoTracking() on f.PetId equals p.Id
                    where f.UserId == userId
                    select new { f, p };

        if (petTypeId.HasValue)
            query = query.Where(x => x.p.PetTypeId == petTypeId.Value);

        if (petStatus is not null && Enum.TryParse<PetStatus>(petStatus, true, out var status))
            query = query.Where(x => x.p.Status == status);

        var total = await query.LongCountAsync();

        var orderedQuery = sortBy switch
        {
            "oldest" => query.OrderBy(x => x.f.CreatedAt),
            "name" => query.OrderBy(x => x.p.Name.Value),
            _ => query.OrderByDescending(x => x.f.CreatedAt)
        };

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
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
