using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record MarkChatThreadReadCommand(
    Guid RequestId,
    Guid CallerId,
    string? CallerRole,
    Guid? CallerOrgId) : IRequest<MarkChatThreadReadResponse>;
