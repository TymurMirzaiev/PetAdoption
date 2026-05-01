using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class AdoptionRequestRepository : IAdoptionRequestRepository
{
    private readonly PetServiceDbContext _context;

    public AdoptionRequestRepository(PetServiceDbContext context)
    {
        _context = context;
    }

    public async Task<AdoptionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AdoptionRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<AdoptionRequest?> GetPendingByUserAndPetAsync(Guid userId, Guid petId, CancellationToken ct = default)
    {
        return await _context.AdoptionRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PetId == petId && r.Status == AdoptionRequestStatus.Pending, ct);
    }

    public async Task AddAsync(AdoptionRequest request, CancellationToken ct = default)
    {
        await _context.AdoptionRequests.AddAsync(request, ct);
        AddOutboxEvents(request);
        await _context.SaveChangesAsync(ct);
        request.ClearDomainEvents();
    }

    public async Task UpdateAsync(AdoptionRequest request, CancellationToken ct = default)
    {
        _context.AdoptionRequests.Update(request);
        AddOutboxEvents(request);
        await _context.SaveChangesAsync(ct);
        request.ClearDomainEvents();
    }

    private void AddOutboxEvents(AdoptionRequest aggregate)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            var eventData = JsonSerializer.Serialize(
                domainEvent,
                domainEvent.GetType(),
                new JsonSerializerOptions { WriteIndented = false });

            _context.OutboxEvents.Add(new OutboxEvent(domainEvent, eventData));
        }
    }
}
