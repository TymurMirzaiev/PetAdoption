namespace PetAdoption.UserService.Domain.ValueObjects;

public record Password
{
    public string HashedValue { get; init; }

    private Password(string hashedValue) => HashedValue = hashedValue;

    /// <summary>
    /// Create Password from already hashed value (from database or after hashing)
    /// </summary>
    public static Password FromHash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Password hash cannot be empty", nameof(hashedPassword));

        return new Password(hashedPassword);
    }

    /// <summary>
    /// Validate plain text password requirements (actual hashing happens in infrastructure)
    /// </summary>
    public static void ValidatePlainText(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainTextPassword));

        if (plainTextPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters", nameof(plainTextPassword));

        if (plainTextPassword.Length > 100)
            throw new ArgumentException("Password cannot exceed 100 characters", nameof(plainTextPassword));
    }
}
