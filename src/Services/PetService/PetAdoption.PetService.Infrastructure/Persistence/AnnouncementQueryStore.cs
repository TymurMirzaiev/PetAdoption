using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AnnouncementQueryStore : IAnnouncementQueryStore
{
    private readonly IMongoCollection<Announcement> _announcements;

    public AnnouncementQueryStore(IMongoDatabase database)
    {
        _announcements = database.GetCollection<Announcement>("Announcements");
    }

    public async Task<(IEnumerable<AnnouncementListDto> Items, long Total)> GetAllAsync(int skip, int take)
    {
        var filter = Builders<Announcement>.Filter.Empty;
        var total = await _announcements.CountDocumentsAsync(filter);
        var items = await _announcements.Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Limit(take)
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
        var filter = Builders<Announcement>.Filter.Eq(a => a.Id, id);
        var announcement = await _announcements.Find(filter).FirstOrDefaultAsync();
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
        var filter = Builders<Announcement>.Filter.And(
            Builders<Announcement>.Filter.Lte(a => a.StartDate, now),
            Builders<Announcement>.Filter.Gte(a => a.EndDate, now));

        var items = await _announcements.Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(a => new ActiveAnnouncementDto(a.Id, a.Title.Value, a.Body.Value));
    }
}
