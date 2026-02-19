namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class OutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxEvent> _outboxEvents;

    public OutboxRepository(IMongoDatabase database)
    {
        _outboxEvents = database.GetCollection<OutboxEvent>("OutboxEvents");
    }

    public async Task AddAsync(OutboxEvent outboxEvent)
    {
        await _outboxEvents.InsertOneAsync(outboxEvent);
    }

    public async Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize = 100)
    {
        return await _outboxEvents
            .Find(e => !e.IsProcessed)
            .SortBy(e => e.CreatedAt)
            .Limit(batchSize)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(string id)
    {
        var filter = Builders<OutboxEvent>.Filter.Eq(e => e.Id, id);
        var update = Builders<OutboxEvent>.Update
            .Set(e => e.IsProcessed, true)
            .Set(e => e.ProcessedAt, DateTime.UtcNow);

        await _outboxEvents.UpdateOneAsync(filter, update);
    }

    public async Task MarkAsFailedAsync(string id, string error)
    {
        var filter = Builders<OutboxEvent>.Filter.Eq(e => e.Id, id);
        var update = Builders<OutboxEvent>.Update
            .Inc(e => e.RetryCount, 1)
            .Set(e => e.LastError, error);

        await _outboxEvents.UpdateOneAsync(filter, update);
    }
}
