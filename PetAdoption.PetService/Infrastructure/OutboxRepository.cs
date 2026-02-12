using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure;

public class OutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxEvent> _outboxEvents;

    public OutboxRepository(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        var database = client.GetDatabase("PetAdoptionDb");
        _outboxEvents = database.GetCollection<OutboxEvent>("OutboxEvents");
    }

    public async Task Add(OutboxEvent outboxEvent)
    {
        await _outboxEvents.InsertOneAsync(outboxEvent);
    }

    public async Task AddRange(IEnumerable<OutboxEvent> outboxEvents)
    {
        await _outboxEvents.InsertManyAsync(outboxEvents);
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingEvents(int batchSize = 100)
    {
        return await _outboxEvents
            .Find(e => !e.IsProcessed && e.RetryCount < 5)
            .SortBy(e => e.OccurredOn)
            .Limit(batchSize)
            .ToListAsync();
    }

    public async Task Update(OutboxEvent outboxEvent)
    {
        await _outboxEvents.ReplaceOneAsync(e => e.Id == outboxEvent.Id, outboxEvent);
    }
}
