namespace PetAdoption.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class OutboxRepository : IOutboxRepository
{
    private readonly UserServiceDbContext _db;

    public OutboxRepository(UserServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxEvent outboxEvent)
    {
        _db.OutboxEvents.Add(outboxEvent);
        await _db.SaveChangesAsync();
    }

    public async Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize = 100)
    {
        return await _db.OutboxEvents
            .Where(e => !e.IsProcessed && e.RetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(string id)
    {
        var outboxEvent = await _db.OutboxEvents.FindAsync(id);
        if (outboxEvent is null) return;

        outboxEvent.MarkProcessed();
        await _db.SaveChangesAsync();
    }

    public async Task MarkAsFailedAsync(string id, string error)
    {
        var outboxEvent = await _db.OutboxEvents.FindAsync(id);
        if (outboxEvent is null) return;

        outboxEvent.MarkFailed(error);
        await _db.SaveChangesAsync();
    }
}
