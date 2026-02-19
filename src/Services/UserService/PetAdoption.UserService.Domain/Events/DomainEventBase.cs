namespace PetAdoption.UserService.Domain.Events;

public abstract record DomainEventBase
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
