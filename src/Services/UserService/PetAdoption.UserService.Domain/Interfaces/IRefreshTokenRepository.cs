namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAllForUserAsync(string userId);
}
