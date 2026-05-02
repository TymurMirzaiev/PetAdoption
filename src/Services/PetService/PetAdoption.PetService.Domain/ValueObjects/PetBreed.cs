namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetBreed : StringValueObject, IEquatable<PetBreed>
{
    public PetBreed(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidPetBreed, "Breed cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
            throw new DomainException(PetDomainErrorCode.InvalidPetBreed, "Breed cannot exceed 100 characters.");

        Value = trimmed;
    }

    public bool Equals(PetBreed? other) => other is not null && Value == other.Value;

    public static bool operator ==(PetBreed? left, PetBreed? right) => Equals(left, right);
    public static bool operator !=(PetBreed? left, PetBreed? right) => !Equals(left, right);

    public static implicit operator string(PetBreed breed) => breed.Value;
    public static explicit operator PetBreed(string value) => new(value);
}
