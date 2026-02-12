using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

/// <summary>
/// Value object representing a pet's name with validation rules.
/// </summary>
public sealed class PetName : IEquatable<PetName>
{
    public const int MaxLength = 100;
    public const int MinLength = 1;

    public string Value { get; }

    public PetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetName,
                "Pet name cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value ?? "null" },
                    { "Reason", "EmptyOrWhitespace" }
                });
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length < MinLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetName,
                $"Pet name must be at least {MinLength} character(s).",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MinLength", MinLength },
                    { "ActualLength", trimmedValue.Length },
                    { "Reason", "TooShort" }
                });
        }

        if (trimmedValue.Length > MaxLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetName,
                $"Pet name cannot exceed {MaxLength} characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MaxLength", MaxLength },
                    { "ActualLength", trimmedValue.Length },
                    { "Reason", "TooLong" }
                });
        }

        Value = trimmedValue;
    }

    // Equality implementation for value object
    public bool Equals(PetName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as PetName);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    // Implicit conversion from string for convenience
    public static implicit operator string(PetName petName) => petName.Value;

    // Explicit conversion to PetName to enforce validation
    public static explicit operator PetName(string value) => new(value);

    public static bool operator ==(PetName? left, PetName? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PetName? left, PetName? right) => !(left == right);
}
