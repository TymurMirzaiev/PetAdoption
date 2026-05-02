using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Infrastructure.Services;

namespace PetAdoption.PetService.UnitTests.Services;

public class ChatAuthorizationServiceTests
{
    private readonly ChatAuthorizationService _sut = new();

    private static AdoptionRequest CreateRequest(Guid userId, Guid orgId)
    {
        var request = AdoptionRequest.Create(userId, Guid.NewGuid(), orgId);
        return request;
    }

    // ──────────────────────────────────────────────────────────────
    // Adopter path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Adopter_Allowed_ForOwnRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = CreateRequest(userId, Guid.NewGuid());

        // Act
        var (allowed, role) = await _sut.AuthorizeAsync(request, userId, null, null);

        // Assert
        allowed.Should().BeTrue();
        role.Should().Be(ChatSenderRole.Adopter);
    }

    // ──────────────────────────────────────────────────────────────
    // Shelter path
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public async Task Admin_Allowed_ForOwnOrg(string callerRole)
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = CreateRequest(Guid.NewGuid(), orgId);

        // Act
        var (allowed, role) = await _sut.AuthorizeAsync(request, Guid.NewGuid(), callerRole, orgId);

        // Assert
        allowed.Should().BeTrue();
        role.Should().Be(ChatSenderRole.Shelter);
    }

    [Fact]
    public async Task Admin_Denied_ForWrongOrg()
    {
        // Arrange
        var request = CreateRequest(Guid.NewGuid(), Guid.NewGuid());
        var differentOrgId = Guid.NewGuid();

        // Act
        var (allowed, _) = await _sut.AuthorizeAsync(request, Guid.NewGuid(), "Admin", differentOrgId);

        // Assert
        allowed.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Denied paths
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserRole_OrgMember_Denied()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = CreateRequest(Guid.NewGuid(), orgId);

        // Act — "User" role (not Admin/Moderator) should be denied
        var (allowed, _) = await _sut.AuthorizeAsync(request, Guid.NewGuid(), "User", orgId);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Stranger_Denied()
    {
        // Arrange
        var request = CreateRequest(Guid.NewGuid(), Guid.NewGuid());

        // Act — completely unrelated user, no org
        var (allowed, _) = await _sut.AuthorizeAsync(request, Guid.NewGuid(), null, null);

        // Assert
        allowed.Should().BeFalse();
    }
}
