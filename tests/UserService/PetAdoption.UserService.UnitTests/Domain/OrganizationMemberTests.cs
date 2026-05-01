using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.UnitTests.Domain;

public class OrganizationMemberTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreateMember()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var member = OrganizationMember.Create(orgId, userId, OrgRole.Admin);

        // Assert
        member.Id.Should().NotBeEmpty();
        member.OrganizationId.Should().Be(orgId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(OrgRole.Admin);
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyOrgId_ShouldThrow()
    {
        // Act & Assert
        var act = () => OrganizationMember.Create(Guid.Empty, "user-id", OrgRole.Moderator);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidUserId_ShouldThrow(string? userId)
    {
        // Act & Assert
        var act = () => OrganizationMember.Create(Guid.NewGuid(), userId!, OrgRole.Admin);
        act.Should().Throw<ArgumentException>();
    }
}
