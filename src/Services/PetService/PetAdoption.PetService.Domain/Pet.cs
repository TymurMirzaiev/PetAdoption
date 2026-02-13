using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Domain;

public class Pet : IAggregateRoot, IEntity
{
    public Guid Id { get; private set; }
    public PetName Name { get; private set; }
    public Guid PetTypeId { get; private set; }
    public PetStatus Status { get; private set; }
    public int Version { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    // Private parameterless constructor for ORM/MongoDB deserialization
    private Pet() { }

    // Private constructor for creating new pets
    private Pet(Guid id, PetName name, Guid petTypeId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Pet ID cannot be empty.", nameof(id));

        if (petTypeId == Guid.Empty)
            throw new ArgumentException("Pet type ID cannot be empty.", nameof(petTypeId));

        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PetTypeId = petTypeId;
        Status = PetStatus.Available;
    }

    /// <summary>
    /// Factory method to create a new available pet with validated pet type.
    /// </summary>
    public static Pet Create(string name, Guid petTypeId)
    {
        return new Pet(Guid.NewGuid(), new PetName(name), petTypeId);
    }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Reserve()
    {
        if (Status != PetStatus.Available)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotAvailable,
                $"Pet {Id} cannot be reserved because it is {Status}. Only Available pets can be reserved.",
                new Dictionary<string, object>
                {
                    { "PetId", Id },
                    { "CurrentStatus", Status.ToString() },
                    { "RequiredStatus", PetStatus.Available.ToString() }
                });
        }

        Status = PetStatus.Reserved;
        AddDomainEvent(new PetReservedEvent(Id, Name));
    }

    public void Adopt()
    {
        if (Status != PetStatus.Reserved)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotReserved,
                $"Pet {Id} cannot be adopted because it is {Status}. Only Reserved pets can be adopted.",
                new Dictionary<string, object>
                {
                    { "PetId", Id },
                    { "CurrentStatus", Status.ToString() },
                    { "RequiredStatus", PetStatus.Reserved.ToString() }
                });
        }

        Status = PetStatus.Adopted;
        AddDomainEvent(new PetAdoptedEvent(Id, Name));
    }

    public void CancelReservation()
    {
        if (Status != PetStatus.Reserved)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotReserved,
                $"Pet {Id} reservation cannot be cancelled because it is {Status}. Only Reserved pets can have their reservation cancelled.",
                new Dictionary<string, object>
                {
                    { "PetId", Id },
                    { "CurrentStatus", Status.ToString() },
                    { "RequiredStatus", PetStatus.Reserved.ToString() }
                });
        }

        Status = PetStatus.Available;
        AddDomainEvent(new PetReservationCancelledEvent(Id, Name));
    }

    public void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal void IncrementVersion() => Version++;
}
