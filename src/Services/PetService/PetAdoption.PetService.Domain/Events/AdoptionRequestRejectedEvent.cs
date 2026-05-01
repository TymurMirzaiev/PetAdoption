namespace PetAdoption.PetService.Domain;

public class AdoptionRequestRejectedEvent : DomainEventBase
{
    public Guid UserId { get; init; }
    public Guid PetId { get; init; }
    public Guid OrganizationId { get; init; }
    public string Reason { get; init; } = string.Empty;

    public AdoptionRequestRejectedEvent() { }

    public AdoptionRequestRejectedEvent(Guid requestId, Guid userId, Guid petId, Guid organizationId, string reason)
        : base(requestId)
    {
        UserId = userId;
        PetId = petId;
        OrganizationId = organizationId;
        Reason = reason;
    }
}
