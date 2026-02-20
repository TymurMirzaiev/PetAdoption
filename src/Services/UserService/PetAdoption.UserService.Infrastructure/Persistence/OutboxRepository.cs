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
        // Use Filter API for consistency
        var filter = Builders<OutboxEvent>.Filter.Eq("IsProcessed", false);
        var sort = Builders<OutboxEvent>.Sort.Ascending("CreatedAt");

        return await _outboxEvents
            .Find(filter)
            .Sort(sort)
            .Limit(batchSize)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(string id)
    {
        // Use Filter API for consistency
        var filter = Builders<OutboxEvent>.Filter.Eq("_id", id);
        var update = Builders<OutboxEvent>.Update
            .Set("IsProcessed", true)
            .Set("ProcessedAt", DateTime.UtcNow);

        await _outboxEvents.UpdateOneAsync(filter, update);
    }

    public async Task MarkAsFailedAsync(string id, string error)
    {
        // Use Filter API for consistency
        var filter = Builders<OutboxEvent>.Filter.Eq("_id", id);
        var update = Builders<OutboxEvent>.Update
            .Inc("RetryCount", 1)
            .Set("LastError", error);

        await _outboxEvents.UpdateOneAsync(filter, update);
    }
}
