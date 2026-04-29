namespace PetAdoption.PetService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

public class FavoriteQueryStore : IFavoriteQueryStore
{
    private readonly IMongoCollection<Favorite> _favorites;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<PetType> _petTypes;

    public FavoriteQueryStore(IMongoDatabase database)
    {
        _favorites = database.GetCollection<Favorite>("Favorites");
        _pets = database.GetCollection<Pet>("Pets");
        _petTypes = database.GetCollection<PetType>("PetTypes");
    }

    public async Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take)
    {
        var filter = Builders<Favorite>.Filter.Eq("UserId", userId);
        var total = await _favorites.CountDocumentsAsync(filter);

        var favorites = await _favorites.Find(filter)
            .SortByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        var petIds = favorites.Select(f => f.PetId).ToList();
        var petFilter = Builders<Pet>.Filter.In("_id", petIds);
        var pets = await _pets.Find(petFilter).ToListAsync();
        var petDict = pets.ToDictionary(p => p.Id);

        var typeIds = pets.Select(p => p.PetTypeId).Distinct().ToList();
        var typeFilter = Builders<PetType>.Filter.In("_id", typeIds);
        var types = await _petTypes.Find(typeFilter).ToListAsync();
        var typeDict = types.ToDictionary(t => t.Id);

        var items = favorites.Select(f =>
        {
            var pet = petDict.GetValueOrDefault(f.PetId);
            var typeName = pet is not null && typeDict.TryGetValue(pet.PetTypeId, out var pt) ? pt.Name : "Unknown";
            return new FavoriteWithPetDto(
                f.Id, f.PetId,
                pet?.Name.Value ?? "Deleted",
                typeName,
                pet?.Breed?.Value,
                pet?.Age?.Months,
                pet?.Status.ToString() ?? "Unknown",
                f.CreatedAt);
        });

        return (items, total);
    }
}
