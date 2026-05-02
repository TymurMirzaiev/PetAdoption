using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Services;

public interface IChatAuthorizationService
{
    Task<(bool Allowed, ChatSenderRole SenderRole)> AuthorizeAsync(
        AdoptionRequest request, Guid callerId, string? callerRole, Guid? callerOrgId, CancellationToken ct = default);
}
