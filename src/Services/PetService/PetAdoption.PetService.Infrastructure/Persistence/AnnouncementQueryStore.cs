using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AnnouncementQueryStore : IAnnouncementQueryStore
{
    private readonly PetServiceDbContext _db;

    public AnnouncementQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<AnnouncementListDto> Items, long Total)> GetAllAsync(int skip, int take)
    {
        var total = await _db.Announcements.LongCountAsync();

        var items = await _db.Announcements.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var dtos = items.Select(a => new AnnouncementListDto(
            a.Id,
            a.Title.Value,
            a.StartDate,
            a.EndDate,
            now >= a.StartDate && now <= a.EndDate ? "Active" : now < a.StartDate ? "Scheduled" : "Expired",
            a.CreatedAt));

        return (dtos, total);
    }

    public async Task<AnnouncementDetailDto?> GetByIdAsync(Guid id)
    {
        var announcement = await _db.Announcements.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (announcement is null) return null;

        return new AnnouncementDetailDto(
            announcement.Id,
            announcement.Title.Value,
            announcement.Body.Value,
            announcement.StartDate,
            announcement.EndDate,
            announcement.CreatedBy,
            announcement.CreatedAt);
    }

    public async Task<IEnumerable<ActiveAnnouncementDto>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;

        var items = await _db.Announcements.AsNoTracking()
            .Where(a => a.StartDate <= now && a.EndDate >= now)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(a => new ActiveAnnouncementDto(a.Id, a.Title.Value, a.Body.Value));
    }
}
