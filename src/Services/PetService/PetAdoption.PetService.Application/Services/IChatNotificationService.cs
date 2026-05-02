using PetAdoption.PetService.Application.DTOs;

namespace PetAdoption.PetService.Application.Services;

public interface IChatNotificationService
{
    Task NotifyMessageSentAsync(ChatMessageDto message, CancellationToken ct = default);
}
