using System.Text.RegularExpressions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

public sealed class MicrochipId : IEquatable<MicrochipId>
{
    private static readonly Regex AlphanumericRegex = new("^[A-Z0-9]+$", RegexOptions.Compiled);

    public string Value { get; }

    public MicrochipId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(
                PetDomainErrorCode.InvalidMicrochipId,
                "Microchip ID cannot be empty.");

        var trimmed = value.Trim().ToUpperInvariant();

        if (trimmed.Length < 8 || trimmed.Length > 23)
            throw new DomainException(
                PetDomainErrorCode.InvalidMicrochipId,
                $"Microchip ID must be between 8 and 23 characters. Got {trimmed.Length}.");

        if (!AlphanumericRegex.IsMatch(trimmed))
            throw new DomainException(
                PetDomainErrorCode.InvalidMicrochipId,
                "Microchip ID must contain only alphanumeric characters.");

        Value = trimmed;
    }

    public bool Equals(MicrochipId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as MicrochipId);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
