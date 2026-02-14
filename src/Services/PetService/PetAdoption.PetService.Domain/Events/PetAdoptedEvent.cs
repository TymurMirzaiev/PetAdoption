namespace PetAdoption.PetService.Domain;

public class PetAdoptedEvent : DomainEventBase
{
    public string PetName { get; init; } = string.Empty;

    public PetAdoptedEvent()
    {
    }

    public PetAdoptedEvent(Guid petId, string petName) : base(petId)
    {
        PetName = petName;
    }
}
