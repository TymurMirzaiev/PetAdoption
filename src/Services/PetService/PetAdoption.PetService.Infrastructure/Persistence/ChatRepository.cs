using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class ChatRepository : IChatRepository
{
    private readonly PetServiceDbContext _db;

    public ChatRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> MarkThreadReadAsync(Guid adoptionRequestId, Guid callerId, CancellationToken ct = default)
    {
        var count = await _db.ChatMessages
            .Where(m => m.AdoptionRequestId == adoptionRequestId
                && m.SenderUserId != callerId
                && m.ReadByRecipientAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadByRecipientAt, DateTime.UtcNow), ct);
        return count;
    }
}
