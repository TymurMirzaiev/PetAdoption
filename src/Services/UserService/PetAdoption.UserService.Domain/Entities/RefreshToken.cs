namespace PetAdoption.UserService.Domain.Entities;

using System.Security.Cryptography;

public class RefreshToken
{
    public string Id { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(string userId, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
