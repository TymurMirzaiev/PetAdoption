namespace PetAdoption.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class UserQueryStore : IUserQueryStore
{
    private readonly UserServiceDbContext _db;

    public UserQueryStore(UserServiceDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(UserId id)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 50)
    {
        return await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.RegisteredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _db.Users.CountAsync();
    }
}
