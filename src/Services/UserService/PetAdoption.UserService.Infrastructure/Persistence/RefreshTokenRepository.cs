namespace PetAdoption.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly UserServiceDbContext _db;

    public RefreshTokenRepository(UserServiceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(RefreshToken refreshToken)
    {
        var entry = _db.Entry(refreshToken);
        if (entry.State == EntityState.Detached)
        {
            var exists = await _db.RefreshTokens.AnyAsync(rt => rt.Id == refreshToken.Id);
            if (exists)
                _db.RefreshTokens.Update(refreshToken);
            else
                _db.RefreshTokens.Add(refreshToken);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));
    }
}
