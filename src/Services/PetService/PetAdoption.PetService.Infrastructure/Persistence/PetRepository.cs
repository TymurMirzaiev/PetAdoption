using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetRepository : IPetRepository
{
    private readonly PetServiceDbContext _db;

    public PetRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<Pet?> GetById(Guid id)
    {
        return await _db.Pets
            .Include(p => p.Media)
            .Include(p => p.MedicalRecord)
            .ThenInclude(mr => mr!.Vaccinations)
            .Include(p => p.MedicalRecord)
            .ThenInclude(mr => mr!.Allergies)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task Add(Pet pet)
    {
        _db.Pets.Add(pet);
        AddOutboxEvents(pet);
        await _db.SaveChangesAsync();
        pet.ClearDomainEvents();
    }

    public async Task Update(Pet pet)
    {
        pet.IncrementVersion();
        AddOutboxEvents(pet);

        // EF Core 9: newly added owned entities with user-provided non-default keys are
        // discovered via navigation fix-up during DetectChanges and incorrectly assigned
        // Modified state (instead of Added). Pre-register any untracked media as Added
        // before SaveChangesAsync triggers DetectChanges, so EF Core generates INSERT.
        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var media in pet.Media)
            {
                if (_db.Entry(media).State == EntityState.Detached)
                    _db.Entry(media).State = EntityState.Added;
            }
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(
                PetDomainErrorCode.ConcurrencyConflict,
                $"Pet {pet.Id} was modified by another request.",
                new Dictionary<string, object> { { "PetId", pet.Id } });
        }

        pet.ClearDomainEvents();
    }

    public async Task Delete(Guid id)
    {
        await _db.Pets.Where(p => p.Id == id).ExecuteDeleteAsync();
    }

    private void AddOutboxEvents(Pet aggregate)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            var eventData = JsonSerializer.Serialize(
                domainEvent,
                domainEvent.GetType(),
                new JsonSerializerOptions { WriteIndented = false });

            _db.OutboxEvents.Add(new OutboxEvent(domainEvent, eventData));
        }
    }
}
