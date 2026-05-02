namespace PetAdoption.UserService.Domain.ValueObjects;

public class Bio
{
    public string Value { get; }

    private Bio(string value) => Value = value;

    public static Bio? FromOptional(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        if (trimmed.Length > 1000)
            throw new ArgumentException("Bio must not exceed 1000 characters.", nameof(raw));
        return new Bio(trimmed);
    }

    public override string ToString() => Value;
}
