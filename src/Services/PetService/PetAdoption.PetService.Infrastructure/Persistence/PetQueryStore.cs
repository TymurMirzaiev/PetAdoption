using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure.Persistence;

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

    public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
        PetStatus? status,
        Guid? petTypeId,
        int skip,
        int take)
    {
        var builder = Builders<Pet>.Filter;
        var filter = builder.Empty;

        if (status.HasValue)
            filter &= builder.Eq(p => p.Status, status.Value);

        if (petTypeId.HasValue)
            filter &= builder.Eq(p => p.PetTypeId, petTypeId.Value);

        var total = await _pets.CountDocumentsAsync(filter);
        var pets = await _pets.Find(filter)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        return (pets, total);
    }
}
