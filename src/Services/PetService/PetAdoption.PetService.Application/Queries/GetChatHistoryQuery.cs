using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetChatHistoryQuery(
    Guid RequestId,
    Guid? AfterId,
    int Take,
    Guid CallerId,
    string? CallerRole,
    Guid? CallerOrgId) : IRequest<GetChatHistoryResponse>;

public record GetChatHistoryResponse(IReadOnlyList<ChatMessageDto> Messages, bool HasMore);

public class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, GetChatHistoryResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IChatQueryStore _chatQueryStore;
    private readonly IChatAuthorizationService _chatAuthSvc;

    public GetChatHistoryQueryHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IChatQueryStore chatQueryStore,
        IChatAuthorizationService chatAuthSvc)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _chatQueryStore = chatQueryStore;
        _chatAuthSvc = chatAuthSvc;
    }

    public async Task<GetChatHistoryResponse> Handle(
        GetChatHistoryQuery request, CancellationToken ct = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, ct)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        var (allowed, _) = await _chatAuthSvc.AuthorizeAsync(
            adoptionRequest, request.CallerId, request.CallerRole, request.CallerOrgId, ct);

        if (!allowed)
            throw new DomainException(
                PetDomainErrorCode.ChatAccessDenied,
                "Not authorized to view this chat thread.");

        var take = Math.Clamp(request.Take, 1, 100);
        var (messages, hasMore) = await _chatQueryStore.GetHistoryAsync(
            request.RequestId, request.AfterId, take, ct);

        var dtos = messages
            .Select(m => new ChatMessageDto(
                m.Id, m.AdoptionRequestId, m.SenderUserId,
                m.SenderRole.ToString(), m.Body, m.SentAt, m.ReadByRecipientAt))
            .ToList();

        return new GetChatHistoryResponse(dtos, hasMore);
    }
}
