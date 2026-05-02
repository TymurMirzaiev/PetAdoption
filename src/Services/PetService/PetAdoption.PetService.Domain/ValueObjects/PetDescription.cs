namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetDescription : StringValueObject, IEquatable<PetDescription>
{
    public PetDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidPetDescription, "Description cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 2000)
            throw new DomainException(PetDomainErrorCode.InvalidPetDescription, "Description cannot exceed 2000 characters.");

        Value = trimmed;
    }

    public bool Equals(PetDescription? other) => other is not null && Value == other.Value;

    public static bool operator ==(PetDescription? left, PetDescription? right) => Equals(left, right);
    public static bool operator !=(PetDescription? left, PetDescription? right) => !Equals(left, right);

    public static implicit operator string(PetDescription desc) => desc.Value;
}
