namespace PetAdoption.UserService.UnitTests.Domain.Entities;

using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;

public class RefreshTokenTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var token = RefreshToken.Create(userId, TimeSpan.FromDays(30));

        // Assert
        token.Id.Should().NotBeNullOrEmpty();
        token.UserId.Should().Be("user-123");
        token.Token.Should().NotBeNullOrEmpty();
        token.Token.Length.Should().BeGreaterThanOrEqualTo(32);
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        token.IsRevoked.Should().BeFalse();
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithEmptyUserId_ShouldThrow(string? userId)
    {
        // Act & Assert
        var act = () => RefreshToken.Create(userId!, TimeSpan.FromDays(30));
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Revoke
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_ShouldSetIsRevokedTrue()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // IsValid
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));

        // Act & Assert
        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var token = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        token.Revoke();

        // Act & Assert
        token.IsValid.Should().BeFalse();
    }
}
