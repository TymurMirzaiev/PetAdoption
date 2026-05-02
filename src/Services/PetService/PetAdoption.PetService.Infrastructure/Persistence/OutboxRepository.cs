using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class OutboxRepository : IOutboxRepository
{
    private readonly PetServiceDbContext _db;

    public OutboxRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxEvent outboxEvent)
    {
        _db.OutboxEvents.Add(outboxEvent);
        await _db.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<OutboxEvent> outboxEvents)
    {
        _db.OutboxEvents.AddRange(outboxEvents);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 100)
    {
        return await _db.OutboxEvents
            .Where(e => !e.IsProcessed && e.RetryCount < OutboxEvent.MaxRetryCount)
            .OrderBy(e => e.OccurredOn)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task UpdateAsync(OutboxEvent outboxEvent)
    {
        _db.Entry(outboxEvent).State = EntityState.Modified;
        await _db.SaveChangesAsync();
    }
}
