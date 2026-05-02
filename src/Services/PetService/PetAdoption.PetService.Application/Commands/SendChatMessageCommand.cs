using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record SendChatMessageCommand(
    Guid RequestId,
    string Body,
    Guid SenderId,
    string? SenderRole,
    Guid? SenderOrgId) : IRequest<SendChatMessageResponse>;
