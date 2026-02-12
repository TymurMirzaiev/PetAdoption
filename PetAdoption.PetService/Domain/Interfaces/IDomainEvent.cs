namespace PetAdoption.PetService.Domain.Interfaces;

/// <summary>
/// Marker interface for domain events with common metadata.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// ID of the aggregate that raised this event.
    /// </summary>
    Guid AggregateId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
