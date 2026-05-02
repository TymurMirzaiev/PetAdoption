namespace PetAdoption.PetService.Domain.Interfaces;

/// <summary>
/// Repository for managing outbox events.
/// </summary>
public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task AddRangeAsync(IEnumerable<OutboxEvent> outboxEvents);
    Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 100);
    Task UpdateAsync(OutboxEvent outboxEvent);
}
