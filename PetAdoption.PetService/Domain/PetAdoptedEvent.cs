namespace PetAdoption.PetService.Domain;

public class PetAdoptedEvent : DomainEventBase
{
    public string PetName { get; }

    public PetAdoptedEvent(Guid petId, string petName) : base(petId)
    {
        PetName = petName;
    }
}
