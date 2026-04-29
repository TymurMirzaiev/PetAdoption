namespace PetAdoption.PetService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly IMongoCollection<Favorite> _favorites;

    public FavoriteRepository(IMongoDatabase database)
    {
        _favorites = database.GetCollection<Favorite>("Favorites");
        var indexBuilder = Builders<Favorite>.IndexKeys;
        var uniqueIndex = new CreateIndexModel<Favorite>(
            indexBuilder.Ascending("UserId").Ascending("PetId"),
            new CreateIndexOptions { Unique = true });
        _favorites.Indexes.CreateOne(uniqueIndex);
    }

    public async Task AddAsync(Favorite favorite)
    {
        await _favorites.InsertOneAsync(favorite);
    }

    public async Task<Favorite?> GetByUserAndPetAsync(Guid userId, Guid petId)
    {
        var filter = Builders<Favorite>.Filter.And(
            Builders<Favorite>.Filter.Eq("UserId", userId),
            Builders<Favorite>.Filter.Eq("PetId", petId));
        return await _favorites.Find(filter).FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(Guid userId, Guid petId)
    {
        var filter = Builders<Favorite>.Filter.And(
            Builders<Favorite>.Filter.Eq("UserId", userId),
            Builders<Favorite>.Filter.Eq("PetId", petId));
        await _favorites.DeleteOneAsync(filter);
    }
}
