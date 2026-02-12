using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public class Pet : IAggregateRoot, IEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public PetStatus Status { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    // Private parameterless constructor for ORM/MongoDB deserialization
    private Pet() { }

    // Public constructor for creating new pets
    public Pet(Guid id, string name, string type)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Pet ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pet name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Pet type cannot be empty.", nameof(type));

        Id = id;
        Name = name;
        Type = type;
        Status = PetStatus.Available;
    }
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
