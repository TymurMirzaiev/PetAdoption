using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

public interface IChatQueryStore
{
    Task<(IEnumerable<ChatMessage> Messages, bool HasMore)> GetHistoryAsync(
        Guid requestId, Guid? afterId, int take, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid adoptionRequestId, Guid callerId, CancellationToken ct = default);

    Task<int> GetTotalUnreadForUserAsync(Guid userId, CancellationToken ct = default);

    Task<int> GetTotalUnreadForOrgAsync(Guid orgId, CancellationToken ct = default);
}
