using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public class AdoptionRequest : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public AdoptionRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private AdoptionRequest() { }

    public static AdoptionRequest Create(Guid userId, Guid petId, Guid organizationId, string? message = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId cannot be empty.", nameof(petId));
        if (organizationId == Guid.Empty) throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

        var request = new AdoptionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PetId = petId,
            OrganizationId = organizationId,
            Status = AdoptionRequestStatus.Pending,
            Message = message?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        request._domainEvents.Add(new AdoptionRequestCreatedEvent(
            request.Id, request.UserId, request.PetId, request.OrganizationId));

        return request;
    }

    public void Approve()
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be approved because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        Status = AdoptionRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;

        _domainEvents.Add(new AdoptionRequestApprovedEvent(Id, UserId, PetId, OrganizationId));
    }

    public void Reject(string reason)
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be rejected because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidRejectionReason,
                "Rejection reason is required.",
                new Dictionary<string, object> { { "RequestId", Id } });
        }

        Status = AdoptionRequestStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedAt = DateTime.UtcNow;

        _domainEvents.Add(new AdoptionRequestRejectedEvent(Id, UserId, PetId, OrganizationId, RejectionReason));
    }

    public void Cancel()
    {
        if (Status != AdoptionRequestStatus.Pending)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotPending,
                $"Adoption request {Id} cannot be cancelled because it is {Status}.",
                new Dictionary<string, object>
                {
                    { "RequestId", Id },
                    { "CurrentStatus", Status.ToString() }
                });
        }

        Status = AdoptionRequestStatus.Cancelled;
        ReviewedAt = DateTime.UtcNow;

        _domainEvents.Add(new AdoptionRequestCancelledEvent(Id, UserId, PetId, OrganizationId));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
