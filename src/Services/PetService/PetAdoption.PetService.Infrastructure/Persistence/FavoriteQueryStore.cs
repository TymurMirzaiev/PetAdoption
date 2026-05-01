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
        var query = _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(_db.Pets.AsNoTracking(), f => f.PetId, p => p.Id, (f, p) => new { f, p });

        if (petTypeId.HasValue)
            query = query.Where(x => x.p.PetTypeId == petTypeId.Value);

        if (petStatus is not null && Enum.TryParse<PetStatus>(petStatus, true, out var status))
            query = query.Where(x => x.p.Status == status);

        var total = await query.LongCountAsync();

        var orderedQuery = sortBy switch
        {
            "oldest" => query.OrderBy(x => x.f.CreatedAt).ThenBy(x => x.f.Id),
            "name" => query.OrderBy(x => x.p.Name.Value).ThenBy(x => x.f.Id),
            _ => query.OrderByDescending(x => x.f.CreatedAt).ThenBy(x => x.f.Id)
        };

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .Select(x => new FavoriteWithPetDto(
                x.f.Id,
                x.f.PetId,
                x.p.Name.Value,
                _db.PetTypes.Where(pt => pt.Id == x.p.PetTypeId).Select(pt => pt.Name).First(),
                x.p.Breed != null ? x.p.Breed.Value : null,
                x.p.Age != null ? (int?)x.p.Age.Months : null,
                x.p.Status.ToString(),
                x.f.CreatedAt))
            .ToListAsync();

        return (items, total);
    }
}
