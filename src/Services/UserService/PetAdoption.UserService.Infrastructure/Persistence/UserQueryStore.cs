namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Driver;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class UserQueryStore : IUserQueryStore
{
    private readonly IMongoCollection<User> _users;

    public UserQueryStore(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User?> GetByIdAsync(UserId id)
    {
        // Use Filter API instead of LINQ to work with custom serializers
        var filter = Builders<User>.Filter.Eq("_id", id.Value);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        // Use Filter API instead of LINQ to work with custom serializers
        var filter = Builders<User>.Filter.Eq("Email", email.Value);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 50)
    {
        // Use empty filter for "get all"
        var filter = Builders<User>.Filter.Empty;
        var sort = Builders<User>.Sort.Descending("RegisteredAt");

        return await _users
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        var filter = Builders<User>.Filter.Empty;
        return (int)await _users.CountDocumentsAsync(filter);
    }
}
