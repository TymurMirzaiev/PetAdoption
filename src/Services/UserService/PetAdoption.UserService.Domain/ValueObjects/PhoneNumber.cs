namespace PetAdoption.UserService.Domain.ValueObjects;

public record PhoneNumber
{
    public string Value { get; init; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber? FromOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        // Remove common formatting characters
        var cleaned = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (cleaned.Length < 10 || cleaned.Length > 15)
            throw new ArgumentException("Invalid phone number length (must be 10-15 digits)", nameof(value));

        return new PhoneNumber(cleaned);
    }

    public override string ToString() => Value;
}
