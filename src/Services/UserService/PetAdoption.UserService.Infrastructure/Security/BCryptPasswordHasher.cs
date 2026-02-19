namespace PetAdoption.UserService.Infrastructure.Security;

using PetAdoption.UserService.Application.Abstractions;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
    }

    public bool VerifyPassword(string plainTextPassword, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
