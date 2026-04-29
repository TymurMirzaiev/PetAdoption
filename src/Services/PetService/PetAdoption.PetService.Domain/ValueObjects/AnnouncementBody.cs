namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class AnnouncementBody : IEquatable<AnnouncementBody>
{
    public string Value { get; }

    public AnnouncementBody(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementBody, "Body cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 5000)
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementBody, "Body cannot exceed 5000 characters.");

        Value = trimmed;
    }

    public bool Equals(AnnouncementBody? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is AnnouncementBody other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(AnnouncementBody? left, AnnouncementBody? right) => Equals(left, right);
    public static bool operator !=(AnnouncementBody? left, AnnouncementBody? right) => !Equals(left, right);

    public static implicit operator string(AnnouncementBody b) => b.Value;
}
