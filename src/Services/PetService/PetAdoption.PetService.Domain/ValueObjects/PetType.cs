using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

/// <summary>
/// Value object representing a pet's type with constrained valid values.
/// </summary>
public sealed class PetType : IEquatable<PetType>
{
    // ValidTypes must be initialized BEFORE static PetType instances
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dog", "Cat", "Rabbit", "Bird", "Fish", "Hamster"
    };

    public static readonly PetType Dog = new("Dog");
    public static readonly PetType Cat = new("Cat");
    public static readonly PetType Rabbit = new("Rabbit");
    public static readonly PetType Bird = new("Bird");
    public static readonly PetType Fish = new("Fish");
    public static readonly PetType Hamster = new("Hamster");

    public string Value { get; }

    public PetType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value ?? "null" },
                    { "Reason", "EmptyOrWhitespace" }
                });
        }

        var trimmedValue = value.Trim();

        // Find the canonical form (case-insensitive match)
        var canonicalType = ValidTypes.FirstOrDefault(vt =>
            string.Equals(vt, trimmedValue, StringComparison.OrdinalIgnoreCase));

        if (canonicalType == null)
        {
            var validTypesString = string.Join(", ", ValidTypes);
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                $"Pet type must be one of: {validTypesString}.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "ValidTypes", ValidTypes.ToList() },
                    { "Reason", "InvalidType" }
                });
        }

        Value = canonicalType;
    }

    // Equality implementation for value object
    public bool Equals(PetType? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as PetType);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;

    // Implicit conversion from string for convenience
    public static implicit operator string(PetType petType) => petType.Value;

    // Explicit conversion to PetType to enforce validation
    public static explicit operator PetType(string value) => new(value);

    public static bool operator ==(PetType? left, PetType? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PetType? left, PetType? right) => !(left == right);
}
