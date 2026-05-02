using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetMyChatUnreadTotalQuery(Guid UserId) : IRequest<GetChatUnreadTotalResponse>;
public record GetOrgChatUnreadTotalQuery(Guid OrgId) : IRequest<GetChatUnreadTotalResponse>;
public record GetChatUnreadTotalResponse(int TotalUnread);

public class GetMyChatUnreadTotalQueryHandler
    : IRequestHandler<GetMyChatUnreadTotalQuery, GetChatUnreadTotalResponse>
{
    private readonly IChatQueryStore _chatQueryStore;

    public GetMyChatUnreadTotalQueryHandler(IChatQueryStore chatQueryStore)
    {
        _chatQueryStore = chatQueryStore;
    }

    public async Task<GetChatUnreadTotalResponse> Handle(
        GetMyChatUnreadTotalQuery request, CancellationToken ct = default)
    {
        var total = await _chatQueryStore.GetTotalUnreadForUserAsync(request.UserId, ct);
        return new GetChatUnreadTotalResponse(total);
    }
}

public class GetOrgChatUnreadTotalQueryHandler
    : IRequestHandler<GetOrgChatUnreadTotalQuery, GetChatUnreadTotalResponse>
{
    private readonly IChatQueryStore _chatQueryStore;

    public GetOrgChatUnreadTotalQueryHandler(IChatQueryStore chatQueryStore)
    {
        _chatQueryStore = chatQueryStore;
    }

    public async Task<GetChatUnreadTotalResponse> Handle(
        GetOrgChatUnreadTotalQuery request, CancellationToken ct = default)
    {
        var total = await _chatQueryStore.GetTotalUnreadForOrgAsync(request.OrgId, ct);
        return new GetChatUnreadTotalResponse(total);
    }
}
