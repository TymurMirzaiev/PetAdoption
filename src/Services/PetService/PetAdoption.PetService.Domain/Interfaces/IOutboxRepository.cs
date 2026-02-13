namespace PetAdoption.PetService.Domain.Interfaces;

/// <summary>
/// Repository for managing outbox events.
/// </summary>
public interface IOutboxRepository
{
    Task Add(OutboxEvent outboxEvent);
    Task AddRange(IEnumerable<OutboxEvent> outboxEvents);
    Task<IEnumerable<OutboxEvent>> GetPendingEvents(int batchSize = 100);
    Task Update(OutboxEvent outboxEvent);
}
