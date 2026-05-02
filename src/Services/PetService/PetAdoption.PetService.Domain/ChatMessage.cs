using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

public class ChatMessage : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid AdoptionRequestId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public ChatSenderRole SenderRole { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTime SentAt { get; private set; }
    public DateTime? ReadByRecipientAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private ChatMessage() { }

    public static ChatMessage Send(AdoptionRequest request, Guid senderUserId, ChatSenderRole role, string body)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (request.Status == AdoptionRequestStatus.Rejected || request.Status == AdoptionRequestStatus.Cancelled)
            throw new DomainException(PetDomainErrorCode.ChatThreadClosed, "This adoption request is closed.");

        var trimmed = body?.Trim() ?? "";
        if (trimmed.Length == 0 || trimmed.Length > 2000)
            throw new DomainException(PetDomainErrorCode.InvalidChatMessageBody, "Message body must be 1-2000 characters.");

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            AdoptionRequestId = request.Id,
            SenderUserId = senderUserId,
            SenderRole = role,
            Body = trimmed,
            SentAt = DateTime.UtcNow
        };
    }

    public void MarkRead(DateTime when)
    {
        if (ReadByRecipientAt == null) ReadByRecipientAt = when;
    }
}
