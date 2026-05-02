namespace PetAdoption.PetService.Domain.Interfaces;

public interface IChatRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct = default);
    Task<int> MarkThreadReadAsync(Guid adoptionRequestId, Guid callerId, CancellationToken ct = default);
}
