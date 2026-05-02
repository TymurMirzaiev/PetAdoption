using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

public sealed class MedicalNotes : IEquatable<MedicalNotes>
{
    public const int MaxLength = 5000;

    public string Value { get; }

    public MedicalNotes(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(
                PetDomainErrorCode.InvalidMedicalNotes,
                "Medical notes cannot be empty.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new DomainException(
                PetDomainErrorCode.InvalidMedicalNotes,
                $"Medical notes cannot exceed {MaxLength} characters. Got {trimmed.Length}.");

        Value = trimmed;
    }

    public bool Equals(MedicalNotes? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as MedicalNotes);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
