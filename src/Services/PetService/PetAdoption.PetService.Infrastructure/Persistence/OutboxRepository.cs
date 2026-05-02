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

    public async Task Add(OutboxEvent outboxEvent)
    {
        _db.OutboxEvents.Add(outboxEvent);
        await _db.SaveChangesAsync();
    }

    public async Task AddRange(IEnumerable<OutboxEvent> outboxEvents)
    {
        _db.OutboxEvents.AddRange(outboxEvents);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingEvents(int batchSize = 100)
    {
        return await _db.OutboxEvents
            .Where(e => !e.IsProcessed && e.RetryCount < 5)
            .OrderBy(e => e.OccurredOn)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task Update(OutboxEvent outboxEvent)
    {
        _db.Entry(outboxEvent).State = EntityState.Modified;
        await _db.SaveChangesAsync();
    }
}
