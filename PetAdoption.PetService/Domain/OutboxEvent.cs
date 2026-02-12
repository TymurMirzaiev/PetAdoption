using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

/// <summary>
/// Represents a domain event stored in the outbox for reliable publishing.
/// Ensures events are persisted transactionally with aggregate changes.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; }
    public string EventData { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }
    public bool IsProcessed { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    // Private constructor for MongoDB deserialization
    private OutboxEvent() { }

    public OutboxEvent(IDomainEvent domainEvent, string serializedEventData)
    {
        Id = Guid.NewGuid();
        EventType = domainEvent.GetType().Name;
        EventData = serializedEventData;
        OccurredOn = domainEvent.OccurredOn;
        IsProcessed = false;
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedOn = DateTime.UtcNow;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
