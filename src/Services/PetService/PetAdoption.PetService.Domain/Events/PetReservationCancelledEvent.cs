namespace PetAdoption.PetService.Domain;

public class PetReservationCancelledEvent : DomainEventBase
{
    public string PetName { get; }

    public PetReservationCancelledEvent(Guid petId, string petName) : base(petId)
    {
        PetName = petName;
    }
}
