using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IChatAuthorizationService _chatAuth;
    private readonly IAdoptionRequestRepository _requestRepo;

    public ChatHub(IMediator mediator, IChatAuthorizationService chatAuth, IAdoptionRequestRepository requestRepo)
    {
        _mediator = mediator;
        _chatAuth = chatAuth;
        _requestRepo = requestRepo;
    }

    public async Task JoinThread(Guid requestId)
    {
        var callerId = GetCallerId();
        var request = await _requestRepo.GetByIdAsync(requestId)
            ?? throw new HubException("Request not found.");
        var callerRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var callerOrgId = Guid.TryParse(Context.User?.FindFirst("organizationId")?.Value, out var oid) ? oid : (Guid?)null;
        var (allowed, _) = await _chatAuth.AuthorizeAsync(request, callerId, callerRole, callerOrgId);
        if (!allowed) throw new HubException("chat_access_denied");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"req:{requestId}");
    }

    public Task LeaveThread(Guid requestId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"req:{requestId}");

    public async Task SendMessage(Guid requestId, string body)
    {
        var callerId = GetCallerId();
        var callerRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var callerOrgId = Guid.TryParse(Context.User?.FindFirst("organizationId")?.Value, out var oid) ? oid : (Guid?)null;
        var result = await _mediator.Send(new SendChatMessageCommand(requestId, body, callerId, callerRole, callerOrgId));
        // The handler already broadcasts via IChatNotificationService; no need to broadcast again here
    }

    private Guid GetCallerId() =>
        Guid.TryParse(Context.User?.FindFirst("userId")?.Value, out var id)
            ? id
            : throw new HubException("Unauthorized.");
}
