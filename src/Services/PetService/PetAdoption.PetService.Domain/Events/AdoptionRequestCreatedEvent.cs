namespace PetAdoption.PetService.Domain;

public class AdoptionRequestCreatedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public Guid PetId { get; init; }
    public Guid OrganizationId { get; init; }

    public AdoptionRequestCreatedEvent() { }

    public AdoptionRequestCreatedEvent(Guid requestId, Guid userId, Guid petId, Guid organizationId)
        : base(requestId)
    {
        UserId = userId;
        PetId = petId;
        OrganizationId = organizationId;
    }
}
