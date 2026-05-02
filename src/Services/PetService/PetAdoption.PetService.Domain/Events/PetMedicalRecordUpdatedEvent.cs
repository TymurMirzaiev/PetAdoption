namespace PetAdoption.PetService.Domain;

public class PetMedicalRecordUpdatedEvent : DomainEventBase
{
    public DateTime UpdatedAt { get; init; }

    public PetMedicalRecordUpdatedEvent(Guid petId, DateTime updatedAt) : base(petId)
    {
        UpdatedAt = updatedAt;
    }
}
