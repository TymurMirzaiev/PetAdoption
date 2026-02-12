using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

/// <summary>
/// Base class for domain events providing common event metadata.
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; }
    public Guid AggregateId { get; }
    public DateTime OccurredOn { get; }

    protected DomainEventBase(Guid aggregateId)
    {
        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        OccurredOn = DateTime.UtcNow;
    }
}
