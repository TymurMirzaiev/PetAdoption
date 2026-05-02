namespace PetAdoption.PetService.Domain.ValueObjects;

using PetAdoption.PetService.Domain.Exceptions;

public sealed class AnnouncementTitle : StringValueObject, IEquatable<AnnouncementTitle>
{
    public AnnouncementTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementTitle, "Title cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 200)
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementTitle, "Title cannot exceed 200 characters.");

        Value = trimmed;
    }

    public bool Equals(AnnouncementTitle? other) => other is not null && Value == other.Value;

    public static bool operator ==(AnnouncementTitle? left, AnnouncementTitle? right) => Equals(left, right);
    public static bool operator !=(AnnouncementTitle? left, AnnouncementTitle? right) => !Equals(left, right);

    public static implicit operator string(AnnouncementTitle t) => t.Value;
}
