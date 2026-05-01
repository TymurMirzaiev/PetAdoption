using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Infrastructure.Persistence;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly UserServiceDbContext _db;

    public OrganizationRepository(UserServiceDbContext db) => _db = db;

    public async Task<Organization?> GetByIdAsync(Guid id) =>
        await _db.Organizations.FindAsync(id);

    public async Task<Organization?> GetBySlugAsync(string slug) =>
        await _db.Organizations.FirstOrDefaultAsync(o => o.Slug == slug);

    public async Task<(IEnumerable<Organization> Items, long Total)> GetAllAsync(int skip, int take)
    {
        var total = await _db.Organizations.LongCountAsync();
        var items = await _db.Organizations.OrderBy(o => o.Name).Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(Organization organization)
    {
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Organization organization)
    {
        _db.Organizations.Update(organization);
        await _db.SaveChangesAsync();
    }
}
