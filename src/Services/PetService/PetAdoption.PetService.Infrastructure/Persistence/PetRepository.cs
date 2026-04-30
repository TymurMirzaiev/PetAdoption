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
        return await _db.Pets.FindAsync(id);
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

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(
                PetDomainErrorCode.ConcurrencyConflict,
                $"Pet {pet.Id} was modified by another operation. Please reload and try again.",
                new Dictionary<string, object>
                {
                    { "PetId", pet.Id }
                });
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
