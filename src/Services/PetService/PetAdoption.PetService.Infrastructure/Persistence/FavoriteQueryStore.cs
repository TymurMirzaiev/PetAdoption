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
        var query =
            from f in _db.Favorites.AsNoTracking()
            join p in _db.Pets.AsNoTracking() on f.PetId equals p.Id
            join pt in _db.PetTypes.AsNoTracking() on p.PetTypeId equals pt.Id
            where f.UserId == userId
            select new { f, p, pt };

        if (petTypeId.HasValue)
            query = query.Where(x => x.p.PetTypeId == petTypeId.Value);

        if (petStatus is not null && Enum.TryParse<PetStatus>(petStatus, true, out var status))
            query = query.Where(x => x.p.Status == status);

        var total = await query.LongCountAsync();

        var orderedQuery = sortBy switch
        {
            "oldest" => query.OrderBy(x => x.f.CreatedAt).ThenBy(x => x.f.Id),
            "name" => query.OrderBy(x => x.p.Name).ThenBy(x => x.f.Id),
            _ => query.OrderByDescending(x => x.f.CreatedAt).ThenBy(x => x.f.Id)
        };

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .Select(x => new FavoriteWithPetDto(
                x.f.Id,
                x.f.PetId,
                x.p.Name.Value,
                x.pt.Name,
                x.p.Breed != null ? x.p.Breed.Value : null,
                x.p.Age != null ? (int?)x.p.Age.Months : null,
                x.p.Status.ToString(),
                x.f.CreatedAt))
            .ToListAsync();

        return (items, total);
    }
}
