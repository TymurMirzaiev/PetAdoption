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
        return await _users.Find(u => u.Id.Value == id.Value).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _users.Find(u => u.Email.Value == email.Value).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 50)
    {
        return await _users
            .Find(_ => true)
            .SortByDescending(u => u.RegisteredAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return (int)await _users.CountDocumentsAsync(_ => true);
    }
}
