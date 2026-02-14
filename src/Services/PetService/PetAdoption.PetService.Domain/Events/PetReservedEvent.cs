namespace PetAdoption.PetService.Domain;

public class PetReservedEvent : DomainEventBase
{
    public string PetName { get; init; } = string.Empty;

    public PetReservedEvent()
    {
    }

    public PetReservedEvent(Guid petId, string petName) : base(petId)
    {
        PetName = petName;
    }
}
