namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task<List<OutboxEvent>> GetUnprocessedAsync(int batchSize = 100);
    Task MarkAsProcessedAsync(string id);
    Task MarkAsFailedAsync(string id, string error);
}
