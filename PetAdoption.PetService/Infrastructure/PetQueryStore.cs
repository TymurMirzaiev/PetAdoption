using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public class PetQueryStore : IPetQueryStore
{
    private readonly IMongoCollection<Pet> _pets;

    public PetQueryStore(IConfiguration configuration)
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
}
