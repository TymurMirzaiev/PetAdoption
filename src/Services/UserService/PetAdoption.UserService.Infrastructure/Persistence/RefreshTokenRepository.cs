namespace PetAdoption.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class RefreshTokenRepository : RepositoryBase, IRefreshTokenRepository
{
    public RefreshTokenRepository(UserServiceDbContext db) : base(db)
    {
    }

    public async Task SaveAsync(RefreshToken refreshToken)
    {
        await UpsertAsync(_db.RefreshTokens, refreshToken, rt => rt.Id == refreshToken.Id);
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
