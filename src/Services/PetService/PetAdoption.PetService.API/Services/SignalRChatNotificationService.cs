using Microsoft.AspNetCore.SignalR;
using PetAdoption.PetService.API.Hubs;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Services;

namespace PetAdoption.PetService.API.Services;

public class SignalRChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatNotificationService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyMessageSentAsync(ChatMessageDto message, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"req:{message.AdoptionRequestId}")
            .SendAsync("MessageReceived", message, ct);
    }
}
