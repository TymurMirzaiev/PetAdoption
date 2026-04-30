using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly PetServiceDbContext _db;

    public AnnouncementRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        return await _db.Announcements.FindAsync(id);
    }

    public async Task AddAsync(Announcement announcement)
    {
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Announcement announcement)
    {
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _db.Announcements.Where(a => a.Id == id).ExecuteDeleteAsync();
    }
}
