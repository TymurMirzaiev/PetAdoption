using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record SendChatMessageResponse(ChatMessageDto Message);

public class SendChatMessageCommandHandler
    : IRequestHandler<SendChatMessageCommand, SendChatMessageResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IChatAuthorizationService _chatAuthSvc;
    private readonly IChatNotificationService _notificationService;

    public SendChatMessageCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IChatRepository chatRepository,
        IChatAuthorizationService chatAuthSvc,
        IChatNotificationService notificationService)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _chatRepository = chatRepository;
        _chatAuthSvc = chatAuthSvc;
        _notificationService = notificationService;
    }

    public async Task<SendChatMessageResponse> Handle(
        SendChatMessageCommand command, CancellationToken ct = default)
    {
        var request = await _adoptionRequestRepository.GetByIdAsync(command.RequestId, ct)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {command.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", command.RequestId } });

        var (allowed, senderRole) = await _chatAuthSvc.AuthorizeAsync(
            request, command.SenderId, command.SenderRole, command.SenderOrgId, ct);

        if (!allowed)
            throw new DomainException(
                PetDomainErrorCode.ChatAccessDenied,
                "Not authorized to send messages in this thread.");

        var message = ChatMessage.Send(request, command.SenderId, senderRole, command.Body);
        await _chatRepository.AddAsync(message, ct);

        var dto = MapToDto(message);
        await _notificationService.NotifyMessageSentAsync(dto, ct);

        return new SendChatMessageResponse(dto);
    }

    internal static ChatMessageDto MapToDto(ChatMessage message) =>
        new(message.Id, message.AdoptionRequestId, message.SenderUserId,
            message.SenderRole.ToString(), message.Body, message.SentAt, message.ReadByRecipientAt);
}
