using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record MarkChatThreadReadResponse(int Marked);

public class MarkChatThreadReadCommandHandler
    : IRequestHandler<MarkChatThreadReadCommand, MarkChatThreadReadResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IChatAuthorizationService _chatAuthSvc;

    public MarkChatThreadReadCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IChatRepository chatRepository,
        IChatAuthorizationService chatAuthSvc)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _chatRepository = chatRepository;
        _chatAuthSvc = chatAuthSvc;
    }

    public async Task<MarkChatThreadReadResponse> Handle(
        MarkChatThreadReadCommand command, CancellationToken ct = default)
    {
        var request = await _adoptionRequestRepository.GetByIdAsync(command.RequestId, ct)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {command.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", command.RequestId } });

        var (allowed, _) = await _chatAuthSvc.AuthorizeAsync(
            request, command.CallerId, command.CallerRole, command.CallerOrgId, ct);

        if (!allowed)
            throw new DomainException(
                PetDomainErrorCode.ChatAccessDenied,
                "Not authorized to access this chat thread.");

        var marked = await _chatRepository.MarkThreadReadAsync(command.RequestId, command.CallerId, ct);
        return new MarkChatThreadReadResponse(marked);
    }
}
