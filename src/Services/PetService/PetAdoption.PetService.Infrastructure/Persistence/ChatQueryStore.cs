using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class ChatQueryStore : IChatQueryStore
{
    private readonly PetServiceDbContext _db;

    public ChatQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<ChatMessage> Messages, bool HasMore)> GetHistoryAsync(
        Guid requestId, Guid? afterId, int take, CancellationToken ct = default)
    {
        IQueryable<ChatMessage> query = _db.ChatMessages
            .Where(m => m.AdoptionRequestId == requestId);

        if (afterId.HasValue)
        {
            var cursor = await _db.ChatMessages
                .Where(m => m.Id == afterId.Value)
                .Select(m => m.SentAt)
                .FirstOrDefaultAsync(ct);

            if (cursor != default)
                query = query.Where(m => m.SentAt > cursor);
        }

        var results = await query
            .OrderBy(m => m.SentAt)
            .Take(take + 1)
            .ToListAsync(ct);

        var hasMore = results.Count > take;
        return (results.Take(take), hasMore);
    }

    public async Task<int> GetUnreadCountAsync(Guid adoptionRequestId, Guid callerId, CancellationToken ct = default)
    {
        return await _db.ChatMessages
            .CountAsync(m => m.AdoptionRequestId == adoptionRequestId
                && m.SenderUserId != callerId
                && m.ReadByRecipientAt == null, ct);
    }

    public async Task<int> GetTotalUnreadForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.ChatMessages
            .Join(_db.AdoptionRequests,
                m => m.AdoptionRequestId,
                r => r.Id,
                (m, r) => new { m, r })
            .CountAsync(x => x.r.UserId == userId
                && x.m.SenderUserId != userId
                && x.m.ReadByRecipientAt == null, ct);
    }

    public async Task<int> GetTotalUnreadForOrgAsync(Guid orgId, CancellationToken ct = default)
    {
        return await _db.ChatMessages
            .Join(_db.AdoptionRequests,
                m => m.AdoptionRequestId,
                r => r.Id,
                (m, r) => new { m, r })
            .CountAsync(x => x.r.OrganizationId == orgId
                && x.m.SenderRole != ChatSenderRole.Shelter
                && x.m.ReadByRecipientAt == null, ct);
    }
}
