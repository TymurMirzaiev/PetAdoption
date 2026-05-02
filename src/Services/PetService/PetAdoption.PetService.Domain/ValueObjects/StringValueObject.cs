namespace PetAdoption.PetService.Domain.ValueObjects;

/// <summary>
/// Base class for string-backed value objects. Subclasses validate and set Value in their
/// constructors; this base provides the structural equality, hash code, and ToString
/// implementations that all string value objects share.
/// </summary>
public abstract class StringValueObject
{
    public string Value { get; protected init; } = string.Empty;

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Value == ((StringValueObject)obj).Value;
    }

    public override int GetHashCode() => Value.GetHashCode();
}
