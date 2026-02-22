namespace PetAdoption.UserService.Tests.Domain.Entities;

using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Events;

public class UserTests
{
    [Fact]
    public void Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var fullName = "John Doe";
        var hashedPassword = "hashed_password";

        // Act
        var user = User.Register(email, fullName, hashedPassword);

        // Assert
        user.Should().NotBeNull();
        user.Email.Value.Should().Be(email);
        user.FullName.Value.Should().Be(fullName);
        user.Role.Should().Be(UserRole.User);
        user.Status.Should().Be(UserStatus.Active);
        user.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Register_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange
        var email = "test@example.com";
        var fullName = "John Doe";
        var hashedPassword = "hashed_password";

        // Act
        var user = User.Register(email, fullName, hashedPassword);

        // Assert
        var domainEvent = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>().Subject;

        domainEvent.UserId.Should().Be(user.Id.Value);
        domainEvent.Email.Should().Be(email);
        domainEvent.FullName.Should().Be(fullName);
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed_password");
        var newFullName = "Jane Smith";
        var newPhoneNumber = "+1234567890";

        // Act
        user.UpdateProfile(newFullName, newPhoneNumber, null);

        // Assert
        user.FullName.Value.Should().Be(newFullName);
        user.PhoneNumber?.Value.Should().Be(newPhoneNumber);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseUserProfileUpdatedEvent()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed_password");
        user.ClearDomainEvents(); // Clear registration event

        // Act
        user.UpdateProfile("Jane Smith", null, null);

        // Assert
        var domainEvent = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileUpdatedEvent>().Subject;

        domainEvent.UserId.Should().Be(user.Id.Value);
    }

    [Fact]
    public void ChangePassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "old_hashed");
        var newHashedPassword = "new_hashed_password";

        // Act
        user.ChangePassword(newHashedPassword);

        // Assert
        user.Password.HashedValue.Should().Be(newHashedPassword);
    }

    [Fact]
    public void ChangePassword_ShouldRaisePasswordChangedEvent()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "old_hashed");
        user.ClearDomainEvents();

        // Act
        user.ChangePassword("new_hashed");

        // Assert
        var domainEvent = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserPasswordChangedEvent>().Subject;

        domainEvent.UserId.Should().Be(user.Id.Value);
    }

    [Fact]
    public void PromoteToAdmin_ShouldChangeRoleToAdmin()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed");
        user.Role.Should().Be(UserRole.User);

        // Act
        user.PromoteToAdmin();

        // Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void PromoteToAdmin_ShouldRaiseUserPromotedToAdminEvent()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed");
        user.ClearDomainEvents();

        // Act
        user.PromoteToAdmin();

        // Assert
        var domainEvent = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRoleChangedEvent>().Subject;

        domainEvent.UserId.Should().Be(user.Id.Value);
    }

    [Fact]
    public void Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed");
        var reason = "Violation of terms";

        // Act
        user.Suspend(reason);

        // Assert
        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void Suspend_ShouldRaiseUserSuspendedEvent()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed");
        user.ClearDomainEvents();
        var reason = "Violation of terms";

        // Act
        user.Suspend(reason);

        // Assert
        var domainEvent = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserSuspendedEvent>().Subject;

        domainEvent.UserId.Should().Be(user.Id.Value);
        domainEvent.Reason.Should().Be(reason);
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginTime()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "hashed");
        user.LastLoginAt.Should().BeNull();

        // Act
        user.RecordLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
