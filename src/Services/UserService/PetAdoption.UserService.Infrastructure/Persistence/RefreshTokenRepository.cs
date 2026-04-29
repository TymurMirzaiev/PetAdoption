namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _tokens;

    public RefreshTokenRepository(IMongoDatabase database)
    {
        _tokens = database.GetCollection<RefreshToken>("RefreshTokens");

        var indexBuilder = Builders<RefreshToken>.IndexKeys;
        _tokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            indexBuilder.Ascending("Token"),
            new CreateIndexOptions { Unique = true }));
        _tokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            indexBuilder.Ascending("UserId")));
    }

    public async Task SaveAsync(RefreshToken refreshToken)
    {
        var filter = Builders<RefreshToken>.Filter.Eq("_id", refreshToken.Id);
        await _tokens.ReplaceOneAsync(filter, refreshToken, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        var filter = Builders<RefreshToken>.Filter.Eq("Token", token);
        return await _tokens.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var filter = Builders<RefreshToken>.Filter.And(
            Builders<RefreshToken>.Filter.Eq("UserId", userId),
            Builders<RefreshToken>.Filter.Eq("IsRevoked", false));
        var update = Builders<RefreshToken>.Update.Set("IsRevoked", true);
        await _tokens.UpdateManyAsync(filter, update);
    }
}
