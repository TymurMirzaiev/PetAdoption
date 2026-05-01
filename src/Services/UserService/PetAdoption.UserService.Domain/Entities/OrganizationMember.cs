using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string UserId { get; private set; } = null!;
    public OrgRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private OrganizationMember() { }

    public static OrganizationMember Create(Guid organizationId, string userId, OrgRole role)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}
