namespace PetAdoption.UserService.Application.Abstractions;

/// <summary>
/// Service for hashing and verifying passwords
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    string HashPassword(string plainTextPassword);

    /// <summary>
    /// Verify a plain text password against a hash
    /// </summary>
    bool VerifyPassword(string plainTextPassword, string hashedPassword);
}
