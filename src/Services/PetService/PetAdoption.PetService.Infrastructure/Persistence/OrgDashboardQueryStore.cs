using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class OrgDashboardQueryStore : IOrgDashboardQueryStore
{
    private readonly PetServiceDbContext _db;

    public OrgDashboardQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<OrgDashboardData> GetDashboardAsync(Guid orgId, CancellationToken ct)
    {
        // 1. Pet counts grouped by status
        var petGroups = await _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalPets = petGroups.Sum(g => g.Count);
        var availablePets = petGroups.FirstOrDefault(g => g.Status == PetStatus.Available)?.Count ?? 0;
        var reservedPets = petGroups.FirstOrDefault(g => g.Status == PetStatus.Reserved)?.Count ?? 0;
        var adoptedPets = petGroups.FirstOrDefault(g => g.Status == PetStatus.Adopted)?.Count ?? 0;

        // 2. Adoption request counts grouped by status
        var requestGroups = await _db.AdoptionRequests.AsNoTracking()
            .Where(r => r.OrganizationId == orgId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalRequests = requestGroups.Sum(g => g.Count);
        var pendingRequests = requestGroups.FirstOrDefault(g => g.Status == AdoptionRequestStatus.Pending)?.Count ?? 0;

        // 3+4. Total impressions and swipes for org pets via join (avoids correlated subqueries)
        var orgPetIds = _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Select(p => p.Id);

        var interactionCounts = await _db.PetInteractions.AsNoTracking()
            .Where(pi => orgPetIds.Contains(pi.PetId)
                && (pi.Type == InteractionType.Impression || pi.Type == InteractionType.Swipe))
            .GroupBy(pi => pi.Type)
            .Select(g => new { Type = g.Key, Count = (long)g.Count() })
            .ToListAsync(ct);

        var totalImpressions = interactionCounts
            .FirstOrDefault(x => x.Type == InteractionType.Impression)?.Count ?? 0L;
        var totalSwipes = interactionCounts
            .FirstOrDefault(x => x.Type == InteractionType.Swipe)?.Count ?? 0L;

        return new OrgDashboardData(
            totalPets,
            availablePets,
            reservedPets,
            adoptedPets,
            totalRequests,
            pendingRequests,
            totalImpressions,
            totalSwipes);
    }

    public async Task<OrgDashboardTrendsData> GetTrendsAsync(
        Guid orgId, DateTime from, DateTime to, CancellationToken ct)
    {
        // Adoptions by week using Pet.AdoptedAt
        var adoptionGroups = await _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == orgId
                && p.Status == PetStatus.Adopted
                && p.AdoptedAt >= from && p.AdoptedAt <= to)
            .GroupBy(p => EF.Functions.DateDiffDay(from, p.AdoptedAt!.Value) / 7)
            .Select(g => new { WeekOffset = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Requests by week using AdoptionRequest.CreatedAt
        var requestGroups = await _db.AdoptionRequests.AsNoTracking()
            .Where(r => r.OrganizationId == orgId
                && r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => EF.Functions.DateDiffDay(from, r.CreatedAt) / 7)
            .Select(g => new { WeekOffset = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var adoptions = adoptionGroups
            .Select(g => (g.WeekOffset, g.Count))
            .ToList()
            .AsReadOnly();

        var requests = requestGroups
            .Select(g => (g.WeekOffset, g.Count))
            .ToList()
            .AsReadOnly();

        return new OrgDashboardTrendsData(
            (IReadOnlyList<(int WeekOffset, int Count)>)adoptions,
            (IReadOnlyList<(int WeekOffset, int Count)>)requests);
    }
}
