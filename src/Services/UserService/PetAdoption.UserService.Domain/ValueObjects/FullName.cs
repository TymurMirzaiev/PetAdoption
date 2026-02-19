namespace PetAdoption.UserService.Domain.ValueObjects;

public record FullName
{
    public string Value { get; init; }

    private FullName(string value) => Value = value;

    public static FullName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty", nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length < 2)
            throw new ArgumentException("Name must be at least 2 characters", nameof(value));

        if (trimmed.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters", nameof(value));

        return new FullName(trimmed);
    }

    public override string ToString() => Value;
}
