using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetMetricsQueryStore : IPetMetricsQueryStore
{
    private readonly PetServiceDbContext _db;

    public PetMetricsQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PetMetricsSummary>> GetMetricsByOrgAsync(
        Guid orgId, DateTime? from, DateTime? to, string? sortBy, bool descending)
    {
        var orgPetsExist = await _db.Pets.AsNoTracking()
            .AnyAsync(p => p.OrganizationId == orgId);

        if (!orgPetsExist)
            return Enumerable.Empty<PetMetricsSummary>();

        var interactionsQuery =
            from interaction in _db.PetInteractions.AsNoTracking()
            join pet in _db.Pets.AsNoTracking() on interaction.PetId equals pet.Id
            where pet.OrganizationId == orgId
            select interaction;

        if (from.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt >= from.Value);
        if (to.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt <= to.Value);

        var interactionCounts = await interactionsQuery
            .GroupBy(pi => new { pi.PetId, pi.Type })
            .Select(g => new
            {
                g.Key.PetId,
                g.Key.Type,
                Count = g.LongCount()
            })
            .ToListAsync();

        var favoriteCounts = await (
            from f in _db.Favorites.AsNoTracking()
            join pet in _db.Pets.AsNoTracking() on f.PetId equals pet.Id
            where pet.OrganizationId == orgId
            group f by f.PetId into g
            select new { PetId = g.Key, Count = g.LongCount() }
        ).ToListAsync();

        var favoriteDict = favoriteCounts.ToDictionary(x => x.PetId, x => x.Count);

        var pets = await _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Join(_db.PetTypes.AsNoTracking(), p => p.PetTypeId, pt => pt.Id,
                (p, pt) => new { p.Id, PetName = p.Name.Value, PetType = pt.Name })
            .ToListAsync();

        var petDict = pets.ToDictionary(p => p.Id);
        var petIds = pets.Select(p => p.Id).ToList();

        var result = new List<PetMetricsSummary>();
        foreach (var petId in petIds)
        {
            if (!petDict.TryGetValue(petId, out var petInfo)) continue;

            var impressions = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Impression)
                .Sum(x => x.Count);
            var swipes = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Swipe)
                .Sum(x => x.Count);
            var rejections = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Rejection)
                .Sum(x => x.Count);
            var favorites = favoriteDict.GetValueOrDefault(petId, 0);

            var swipeRate = impressions > 0 ? (double)swipes / impressions : 0;
            var rejectionRate = impressions > 0 ? (double)rejections / impressions : 0;

            result.Add(new PetMetricsSummary(
                petId, petInfo.PetName, petInfo.PetType,
                impressions, swipes, rejections, favorites,
                Math.Round(swipeRate, 4), Math.Round(rejectionRate, 4)));
        }

        return SortMetrics(result, sortBy, descending);
    }

    public async Task<PetMetricsSummary?> GetMetricsByPetAsync(
        Guid petId, DateTime? from, DateTime? to)
    {
        var pet = await _db.Pets.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId);

        if (pet is null) return null;

        var metrics = await BuildMetricsQuery(new List<Guid> { petId }, from, to);
        return metrics.FirstOrDefault();
    }

    private async Task<List<PetMetricsSummary>> BuildMetricsQuery(
        List<Guid> petIds, DateTime? from, DateTime? to)
    {
        var interactionsQuery = _db.PetInteractions.AsNoTracking()
            .Where(pi => petIds.Contains(pi.PetId));

        if (from.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt >= from.Value);
        if (to.HasValue)
            interactionsQuery = interactionsQuery.Where(pi => pi.CreatedAt <= to.Value);

        var interactionCounts = await interactionsQuery
            .GroupBy(pi => new { pi.PetId, pi.Type })
            .Select(g => new
            {
                g.Key.PetId,
                g.Key.Type,
                Count = g.LongCount()
            })
            .ToListAsync();

        var favoriteCounts = await _db.Favorites.AsNoTracking()
            .Where(f => petIds.Contains(f.PetId))
            .GroupBy(f => f.PetId)
            .Select(g => new { PetId = g.Key, Count = g.LongCount() })
            .ToListAsync();

        var favoriteDict = favoriteCounts.ToDictionary(x => x.PetId, x => x.Count);

        var pets = await _db.Pets.AsNoTracking()
            .Where(p => petIds.Contains(p.Id))
            .Join(_db.PetTypes.AsNoTracking(), p => p.PetTypeId, pt => pt.Id,
                (p, pt) => new { p.Id, PetName = p.Name.Value, PetType = pt.Name })
            .ToListAsync();

        var petDict = pets.ToDictionary(p => p.Id);

        var result = new List<PetMetricsSummary>();
        foreach (var petId in petIds)
        {
            if (!petDict.TryGetValue(petId, out var petInfo)) continue;

            var impressions = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Impression)
                .Sum(x => x.Count);
            var swipes = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Swipe)
                .Sum(x => x.Count);
            var rejections = interactionCounts
                .Where(x => x.PetId == petId && x.Type == InteractionType.Rejection)
                .Sum(x => x.Count);
            var favorites = favoriteDict.GetValueOrDefault(petId, 0);

            var swipeRate = impressions > 0 ? (double)swipes / impressions : 0;
            var rejectionRate = impressions > 0 ? (double)rejections / impressions : 0;

            result.Add(new PetMetricsSummary(
                petId, petInfo.PetName, petInfo.PetType,
                impressions, swipes, rejections, favorites,
                Math.Round(swipeRate, 4), Math.Round(rejectionRate, 4)));
        }

        return result;
    }

    private static IEnumerable<PetMetricsSummary> SortMetrics(
        List<PetMetricsSummary> metrics, string? sortBy, bool descending)
    {
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "impressions" => descending
                ? metrics.OrderByDescending(m => m.ImpressionCount)
                : metrics.OrderBy(m => m.ImpressionCount),
            "swipes" => descending
                ? metrics.OrderByDescending(m => m.SwipeCount)
                : metrics.OrderBy(m => m.SwipeCount),
            "rejections" => descending
                ? metrics.OrderByDescending(m => m.RejectionCount)
                : metrics.OrderBy(m => m.RejectionCount),
            "favorites" => descending
                ? metrics.OrderByDescending(m => m.FavoriteCount)
                : metrics.OrderBy(m => m.FavoriteCount),
            "swiperate" => descending
                ? metrics.OrderByDescending(m => m.SwipeRate)
                : metrics.OrderBy(m => m.SwipeRate),
            "rejectionrate" => descending
                ? metrics.OrderByDescending(m => m.RejectionRate)
                : metrics.OrderBy(m => m.RejectionRate),
            "name" => descending
                ? metrics.OrderByDescending(m => m.PetName)
                : metrics.OrderBy(m => m.PetName),
            _ => descending
                ? metrics.OrderByDescending(m => m.ImpressionCount)
                : metrics.OrderBy(m => m.ImpressionCount)
        };

        return sorted.ToList();
    }
}
