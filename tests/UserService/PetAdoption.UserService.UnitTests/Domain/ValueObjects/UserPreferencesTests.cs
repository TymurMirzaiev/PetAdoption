namespace PetAdoption.UserService.Tests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class UserPreferencesTests
{
    [Fact]
    public void Default_ShouldReturnDefaultPreferences()
    {
        // Act
        var preferences = UserPreferences.Default();

        // Assert
        preferences.Should().NotBeNull();
        preferences.ReceiveEmailNotifications.Should().BeTrue();
        preferences.ReceiveSmsNotifications.Should().BeFalse();
        preferences.PreferredPetType.Should().BeNull();
        preferences.PreferredSizes.Should().BeNull();
        preferences.PreferredAgeRange.Should().BeNull();
    }

    [Fact]
    public void Create_WithCustomValues_ShouldSucceed()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            PreferredPetType = "Dog",
            PreferredSizes = new List<string> { "Small", "Medium" },
            PreferredAgeRange = "Young",
            ReceiveEmailNotifications = false,
            ReceiveSmsNotifications = true
        };

        // Assert
        preferences.PreferredPetType.Should().Be("Dog");
        preferences.PreferredSizes.Should().BeEquivalentTo(new[] { "Small", "Medium" });
        preferences.PreferredAgeRange.Should().Be("Young");
        preferences.ReceiveEmailNotifications.Should().BeFalse();
        preferences.ReceiveSmsNotifications.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSamePreferences_ShouldBeEqual()
    {
        // Arrange
        var pref1 = new UserPreferences
        {
            PreferredPetType = "Cat",
            ReceiveEmailNotifications = true,
            ReceiveSmsNotifications = false
        };

        var pref2 = new UserPreferences
        {
            PreferredPetType = "Cat",
            ReceiveEmailNotifications = true,
            ReceiveSmsNotifications = false
        };

        // Act & Assert
        pref1.Should().Be(pref2);
    }

    [Fact]
    public void Equals_WithDifferentPreferences_ShouldNotBeEqual()
    {
        // Arrange
        var pref1 = new UserPreferences
        {
            PreferredPetType = "Dog",
            ReceiveEmailNotifications = true
        };

        var pref2 = new UserPreferences
        {
            PreferredPetType = "Cat",
            ReceiveEmailNotifications = true
        };

        // Act & Assert
        pref1.Should().NotBe(pref2);
    }

    [Fact]
    public void Create_WithOnlyEmailNotifications_ShouldSucceed()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            ReceiveEmailNotifications = true
        };

        // Assert
        preferences.ReceiveEmailNotifications.Should().BeTrue();
        preferences.ReceiveSmsNotifications.Should().BeFalse(); // Default value
    }

    [Fact]
    public void Create_WithAllPreferences_ShouldSucceed()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            PreferredPetType = "Bird",
            PreferredSizes = new List<string> { "Small" },
            PreferredAgeRange = "Adult",
            ReceiveEmailNotifications = true,
            ReceiveSmsNotifications = true
        };

        // Assert
        preferences.PreferredPetType.Should().Be("Bird");
        preferences.PreferredSizes.Should().ContainSingle().Which.Should().Be("Small");
        preferences.PreferredAgeRange.Should().Be("Adult");
        preferences.ReceiveEmailNotifications.Should().BeTrue();
        preferences.ReceiveSmsNotifications.Should().BeTrue();
    }
}
