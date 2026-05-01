namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class PetAge : IEquatable<PetAge>, IComparable<PetAge>
{
    public int Months { get; }

    public PetAge(int months)
    {
        if (months < 0)
            throw new DomainException(PetDomainErrorCode.InvalidPetAge, "Age cannot be negative.");

        Months = months;
    }

    public bool Equals(PetAge? other) => other is not null && Months == other.Months;
    public override bool Equals(object? obj) => obj is PetAge other && Equals(other);
    public override int GetHashCode() => Months.GetHashCode();
    public override string ToString() => $"{Months} months";

    public int CompareTo(PetAge? other) => other is null ? 1 : Months.CompareTo(other.Months);

    public static bool operator ==(PetAge? left, PetAge? right) => Equals(left, right);
    public static bool operator !=(PetAge? left, PetAge? right) => !Equals(left, right);
    public static bool operator <(PetAge? left, PetAge? right) => Comparer<PetAge>.Default.Compare(left, right) < 0;
    public static bool operator <=(PetAge? left, PetAge? right) => Comparer<PetAge>.Default.Compare(left, right) <= 0;
    public static bool operator >(PetAge? left, PetAge? right) => Comparer<PetAge>.Default.Compare(left, right) > 0;
    public static bool operator >=(PetAge? left, PetAge? right) => Comparer<PetAge>.Default.Compare(left, right) >= 0;
}
