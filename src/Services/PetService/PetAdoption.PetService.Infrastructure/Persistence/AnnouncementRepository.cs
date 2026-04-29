using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly IMongoCollection<Announcement> _announcements;

    public AnnouncementRepository(IMongoDatabase database)
    {
        _announcements = database.GetCollection<Announcement>("Announcements");
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        var filter = Builders<Announcement>.Filter.Eq(a => a.Id, id);
        return await _announcements.Find(filter).FirstOrDefaultAsync();
    }

    public async Task AddAsync(Announcement announcement)
    {
        await _announcements.InsertOneAsync(announcement);
    }

    public async Task UpdateAsync(Announcement announcement)
    {
        var filter = Builders<Announcement>.Filter.Eq(a => a.Id, announcement.Id);
        await _announcements.ReplaceOneAsync(filter, announcement);
    }

    public async Task DeleteAsync(Guid id)
    {
        var filter = Builders<Announcement>.Filter.Eq(a => a.Id, id);
        await _announcements.DeleteOneAsync(filter);
    }
}
