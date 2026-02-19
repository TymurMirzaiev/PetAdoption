namespace PetAdoption.UserService.Domain.ValueObjects;

public record Email
{
    public string Value { get; init; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var trimmed = value.Trim().ToLowerInvariant();

        // Basic email validation
        if (!trimmed.Contains('@') || !trimmed.Contains('.'))
            throw new ArgumentException("Invalid email format", nameof(value));

        if (trimmed.Length > 255)
            throw new ArgumentException("Email cannot exceed 255 characters", nameof(value));

        return new Email(trimmed);
    }

    public override string ToString() => Value;
}
