namespace PetAdoption.UserService.UnitTests.Domain.Entities;

using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

public class UserGoogleSsoTests
{
    // ──────────────────────────────────────────────────────────────
    // RegisterFromGoogle
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterFromGoogle_WithValidData_ShouldCreateUser()
    {
        // Arrange & Act
        var user = User.RegisterFromGoogle("test@gmail.com", "Test User");

        // Assert
        user.Email.Value.Should().Be("test@gmail.com");
        user.FullName.Value.Should().Be("Test User");
        user.ExternalProvider.Should().Be("Google");
        user.Password.Should().BeNull();
    }

    [Fact]
    public void RegisterFromGoogle_ShouldHaveActiveStatus()
    {
        // Act
        var user = User.RegisterFromGoogle("test@gmail.com", "Test User");

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void RegisterFromGoogle_ShouldRaiseDomainEvent()
    {
        // Act
        var user = User.RegisterFromGoogle("test@gmail.com", "Test User");

        // Assert
        user.DomainEvents.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // HasPassword
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void HasPassword_ForGoogleUser_ShouldBeFalse()
    {
        // Arrange
        var user = User.RegisterFromGoogle("test@gmail.com", "Test");

        // Act & Assert
        user.HasPassword.Should().BeFalse();
    }

    [Fact]
    public void HasPassword_ForRegularUser_ShouldBeTrue()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test", "$2a$12$hash");

        // Act & Assert
        user.HasPassword.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // ChangePassword (SSO guard)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_ForGoogleUser_ShouldThrow()
    {
        // Arrange
        var user = User.RegisterFromGoogle("test@gmail.com", "Test");

        // Act
        var act = () => user.ChangePassword("$2a$12$newhash");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot change password for SSO user");
    }
}
