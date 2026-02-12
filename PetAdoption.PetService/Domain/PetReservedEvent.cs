using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public class PetReservedEvent : IDomainEvent
{
    public Guid PetId { get; }
    public string PetName { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public PetReservedEvent(Guid petId, string petName)
    {
        PetId = petId;
        PetName = petName;
    }
}
