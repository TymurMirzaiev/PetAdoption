using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Infrastructure.Persistence;

public class OrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly UserServiceDbContext _db;

    public OrganizationMemberRepository(UserServiceDbContext db) => _db = db;

    public async Task AddAsync(OrganizationMember member)
    {
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync();
    }

    public async Task<OrganizationMember?> GetByOrgAndUserAsync(Guid organizationId, string userId) =>
        await _db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);

    public async Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid organizationId) =>
        await _db.OrganizationMembers.Where(m => m.OrganizationId == organizationId).ToListAsync();

    public async Task<IEnumerable<OrganizationMember>> GetByUserAsync(string userId) =>
        await _db.OrganizationMembers.Where(m => m.UserId == userId).ToListAsync();

    public async Task DeleteAsync(OrganizationMember member)
    {
        _db.OrganizationMembers.Remove(member);
        await _db.SaveChangesAsync();
    }
}
