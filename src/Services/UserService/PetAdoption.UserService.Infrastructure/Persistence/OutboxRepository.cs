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
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(string id)
    {
        await _db.OutboxEvents
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsProcessed, true)
                .SetProperty(e => e.ProcessedAt, DateTime.UtcNow));
    }

    public async Task MarkAsFailedAsync(string id, string error)
    {
        await _db.OutboxEvents
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.RetryCount, e => e.RetryCount + 1)
                .SetProperty(e => e.LastError, error));
    }
}
