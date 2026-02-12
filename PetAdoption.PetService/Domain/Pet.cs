using MongoDB.Bson.Serialization.Attributes;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public class Pet : IAggregateRoot, IEntity
{
    [BsonId]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public PetStatus Status { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Reserve()
    {
        if (Status != PetStatus.Available)
            throw new InvalidOperationException("Pet must be available to reserve.");

        Status = PetStatus.Reserved;
        AddDomainEvent(new PetReservedEvent(Id, Name));
    }

    public void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
