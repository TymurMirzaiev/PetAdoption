using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetAdoption.PetService.Application.Options;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Infrastructure.Persistence;

namespace PetAdoption.PetService.Infrastructure.Services;

public class PetRankingService : IPetRankingService
{
    private readonly PetServiceDbContext _db;
    private readonly IOptions<DiscoverOptions> _options;

    public PetRankingService(PetServiceDbContext db, IOptions<DiscoverOptions> options)
    {
        _db = db;
        _options = options;
    }

    public async Task<bool> UserHasEnoughSignalsAsync(Guid userId, CancellationToken ct)
    {
        var favCount = await _db.Favorites.CountAsync(f => f.UserId == userId, ct);
        if (favCount >= 5) return true;
        var skipCount = await _db.PetSkips.CountAsync(s => s.UserId == userId, ct);
        return skipCount >= 10;
    }

    public async Task<IReadOnlyList<Pet>> RankAsync(
        Guid userId, IReadOnlyList<Pet> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0) return candidates;

        var opts = _options.Value;

        // 1. Get favorited and skipped pet IDs
        var favPetIds = await _db.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.PetId)
            .ToListAsync(ct);

        var skipPetIds = await _db.PetSkips
            .Where(s => s.UserId == userId)
            .Select(s => s.PetId)
            .ToListAsync(ct);

        // 2. Load tag info for all signal pets in one batch
        var allSignalIds = favPetIds.Concat(skipPetIds).Distinct().ToList();
        var signalPets = await _db.Pets
            .Where(p => allSignalIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PetTypeId, p.Age, p.Tags })
            .ToListAsync(ct);

        var favPetIdSet = favPetIds.ToHashSet();
        var skipPetIdSet = skipPetIds.ToHashSet();

        // 3. Build user tag vector
        var userVec = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var favPets = signalPets.Where(p => favPetIdSet.Contains(p.Id)).ToList();
        var skipPets = signalPets.Where(p => skipPetIdSet.Contains(p.Id)).ToList();

        foreach (var pet in favPets)
        {
            foreach (var tag in pet.Tags)
            {
                userVec.TryGetValue(tag.Value, out var current);
                userVec[tag.Value] = current + opts.FavoriteWeight;
            }
        }

        foreach (var pet in skipPets)
        {
            foreach (var tag in pet.Tags)
            {
                userVec.TryGetValue(tag.Value, out var current);
                userVec[tag.Value] = current + opts.SkipWeight;
            }
        }

        // 4. Dominant PetTypeId and age bucket from favorites
        var totalFavPets = favPets.Count;
        var dominantPetTypeId = totalFavPets > 0
            ? favPets.GroupBy(p => p.PetTypeId)
                     .OrderByDescending(g => g.Count())
                     .First().Key
            : (Guid?)null;

        var favPetTypeGroups = favPets.GroupBy(p => p.PetTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var favAgeBucketGroups = favPets
            .Where(p => p.Age != null)
            .GroupBy(p => p.Age!.Months / 12)
            .ToDictionary(g => g.Key, g => g.Count());

        // 5. Score each candidate
        var scored = candidates.Select(candidate =>
        {
            var cosine = CosineSimilarity(userVec, candidate.Tags);

            var petTypeBonusRatio = totalFavPets > 0 && favPetTypeGroups.TryGetValue(candidate.PetTypeId, out var ptCount)
                ? (double)ptCount / totalFavPets
                : 0.0;

            var ageBucket = candidate.Age?.Months / 12;
            var ageBucketBonusRatio = totalFavPets > 0 && ageBucket.HasValue
                && favAgeBucketGroups.TryGetValue(ageBucket.Value, out var abCount)
                ? (double)abCount / totalFavPets
                : 0.0;

            var score = cosine
                + opts.PetTypeBonus * petTypeBonusRatio
                + opts.AgeBucketBonus * ageBucketBonusRatio;

            return (Pet: candidate, Score: score);
        }).ToList();

        // 6. Sort descending by score
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        return scored.Select(s => s.Pet).ToList();
    }

    private static double CosineSimilarity(Dictionary<string, double> userVec, IReadOnlyList<PetTag> candidateTags)
    {
        if (userVec.Count == 0 || candidateTags.Count == 0) return 0;

        var dot = candidateTags.Sum(t => userVec.TryGetValue(t.Value, out var w) ? w : 0);
        var userMag = Math.Sqrt(userVec.Values.Sum(v => v * v));
        var candMag = Math.Sqrt(candidateTags.Count); // all weights 1.0

        return (userMag == 0 || candMag == 0) ? 0 : dot / (userMag * candMag);
    }
}
