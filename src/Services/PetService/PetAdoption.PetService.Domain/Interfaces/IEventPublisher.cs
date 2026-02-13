namespace PetAdoption.PetService.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent);
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents);
}
