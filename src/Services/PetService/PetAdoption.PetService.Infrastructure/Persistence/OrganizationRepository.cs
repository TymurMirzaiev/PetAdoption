using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly PetServiceDbContext _db;

    public OrganizationRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task UpsertAsync(Organization org, CancellationToken ct = default)
    {
        var existing = await _db.Organizations
            .FirstOrDefaultAsync(o => o.Id == org.Id, ct);

        if (existing is null)
        {
            _db.Organizations.Add(org);
        }
        else
        {
            // Detach existing tracked instance and attach the new one as modified
            _db.Entry(existing).State = EntityState.Detached;
            _db.Organizations.Update(org);
        }

        await _db.SaveChangesAsync(ct);
    }
}
