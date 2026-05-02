using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

public sealed class Allergy : IEquatable<Allergy>
{
    public const int MaxLength = 100;

    public string Value { get; }

    public Allergy(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(
                PetDomainErrorCode.InvalidAllergy,
                "Allergy cannot be empty.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new DomainException(
                PetDomainErrorCode.InvalidAllergy,
                $"Allergy cannot exceed {MaxLength} characters. Got {trimmed.Length}.");

        Value = trimmed;
    }

    public bool Equals(Allergy? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Allergy);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
