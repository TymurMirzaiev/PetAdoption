using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent);
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents);
}
