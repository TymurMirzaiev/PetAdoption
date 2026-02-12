using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure;

public class PetRepository : IPetRepository
{
    private readonly IMongoCollection<Pet> _pets;

    public PetRepository(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        var database = client.GetDatabase("PetAdoptionDb");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<IEnumerable<Pet>> GetAll()
    {
        return await _pets.Find(_ => true).ToListAsync();
    }

    public async Task<Pet?> GetById(Guid id)
    {
        return await _pets.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task Update(Pet pet)
    {
        await _pets.ReplaceOneAsync(p => p.Id == pet.Id, pet, new ReplaceOptions { IsUpsert = true });
    }
}
