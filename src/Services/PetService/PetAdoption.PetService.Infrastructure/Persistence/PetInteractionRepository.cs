using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetInteractionRepository : IPetInteractionRepository
{
    private readonly PetServiceDbContext _db;

    public PetInteractionRepository(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PetInteraction interaction)
    {
        _db.PetInteractions.Add(interaction);
        await _db.SaveChangesAsync();
    }

    public async Task AddBatchAsync(IEnumerable<PetInteraction> interactions)
    {
        _db.PetInteractions.AddRange(interactions);
        await _db.SaveChangesAsync();
    }
}
