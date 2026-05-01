using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

/// <summary>
/// Value object representing a pet tag. Normalized to lowercase, trimmed, 1-50 chars.
/// </summary>
public sealed class PetTag : IEquatable<PetTag>
{
    public const int MaxLength = 50;
    public const int MinLength = 1;

    public string Value { get; }

    public PetTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                "Pet tag cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value ?? "null" },
                    { "Reason", "EmptyOrWhitespace" }
                });
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                $"Pet tag must be at least {MinLength} character(s).",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MinLength", MinLength },
                    { "ActualLength", normalized.Length },
                    { "Reason", "TooShort" }
                });
        }

        if (normalized.Length > MaxLength)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetTag,
                $"Pet tag cannot exceed {MaxLength} characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedValue", value },
                    { "MaxLength", MaxLength },
                    { "ActualLength", normalized.Length },
                    { "Reason", "TooLong" }
                });
        }

        Value = normalized;
    }

    public bool Equals(PetTag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as PetTag);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(PetTag tag) => tag.Value;
    public static explicit operator PetTag(string value) => new(value);

    public static bool operator ==(PetTag? left, PetTag? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PetTag? left, PetTag? right) => !(left == right);
}
