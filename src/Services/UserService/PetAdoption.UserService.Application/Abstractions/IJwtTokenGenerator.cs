namespace PetAdoption.UserService.Application.Abstractions;

/// <summary>
/// Service for generating JWT authentication tokens
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT token for the user
    /// </summary>
    string GenerateToken(string userId, string email, string role);
}
