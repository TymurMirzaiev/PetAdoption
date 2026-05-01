using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AdoptionRequestQueryStore : IAdoptionRequestQueryStore
{
    private readonly PetServiceDbContext _context;

    public AdoptionRequestQueryStore(PetServiceDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take)
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.LongCountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByOrganizationAsync(
        Guid organizationId, AdoptionRequestStatus? status, int skip, int take)
    {
        var baseQuery = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        if (status.HasValue)
            baseQuery = baseQuery.Where(r => r.Status == status.Value);

        var ordered = baseQuery.OrderByDescending(r => r.CreatedAt);

        var total = await ordered.LongCountAsync();
        var items = await ordered.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByPetAsync(Guid petId, int skip, int take)
    {
        var query = _context.AdoptionRequests
            .AsNoTracking()
            .Where(r => r.PetId == petId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.LongCountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }
}
