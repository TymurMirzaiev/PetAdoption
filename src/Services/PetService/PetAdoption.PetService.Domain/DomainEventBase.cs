using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; }
    public Guid AggregateId { get; init; }
    public DateTime OccurredOn { get; init; }

    protected DomainEventBase()
    {
    }

    protected DomainEventBase(Guid aggregateId)
    {
        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        OccurredOn = DateTime.UtcNow;
    }
}
