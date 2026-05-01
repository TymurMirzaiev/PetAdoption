namespace PetAdoption.PetService.Domain;

public class AdoptionRequestCancelledEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public Guid PetId { get; init; }
    public Guid OrganizationId { get; init; }

    public AdoptionRequestCancelledEvent() { }

    public AdoptionRequestCancelledEvent(Guid requestId, Guid userId, Guid petId, Guid organizationId)
        : base(requestId)
    {
        UserId = userId;
        PetId = petId;
        OrganizationId = organizationId;
    }
}
