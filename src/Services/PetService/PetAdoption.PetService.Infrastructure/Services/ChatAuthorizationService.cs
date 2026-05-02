using PetAdoption.PetService.Application.Services;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Services;

public class ChatAuthorizationService : IChatAuthorizationService
{
    public Task<(bool Allowed, ChatSenderRole SenderRole)> AuthorizeAsync(
        AdoptionRequest request, Guid callerId, string? callerRole, Guid? callerOrgId,
        CancellationToken ct = default)
    {
        if (callerId == request.UserId)
            return Task.FromResult((true, ChatSenderRole.Adopter));

        if ((callerRole == "Admin" || callerRole == "Moderator") && callerOrgId == request.OrganizationId)
            return Task.FromResult((true, ChatSenderRole.Shelter));

        return Task.FromResult((false, ChatSenderRole.Adopter));
    }
}
