namespace PetAdoption.UserService.Tests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class PhoneNumberTests
{
    // ──────────────────────────────────────────────────────────────
    // Valid Creation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void FromOptional_WithValidPhone_ShouldSucceed()
    {
        // Arrange
        var phoneString = "+1234567890";

        // Act
        var phone = PhoneNumber.FromOptional(phoneString);

        // Assert
        phone.Should().NotBeNull();
        phone!.Value.Should().Be("+1234567890");
    }

    [Fact]
    public void FromOptional_WithFormattedPhone_ShouldRemoveFormatting()
    {
        // Arrange
        var phoneString = "+1 (555) 123-4567";

        // Act
        var phone = PhoneNumber.FromOptional(phoneString);

        // Assert
        phone.Should().NotBeNull();
        phone!.Value.Should().Be("+15551234567");
    }

    [Fact]
    public void FromOptional_WithMinimumLength_ShouldSucceed()
    {
        // Arrange
        var phoneString = "1234567890"; // 10 digits

        // Act
        var phone = PhoneNumber.FromOptional(phoneString);

        // Assert
        phone.Should().NotBeNull();
        phone!.Value.Should().Be("1234567890");
    }

    [Fact]
    public void FromOptional_WithMaximumLength_ShouldSucceed()
    {
        // Arrange
        var phoneString = "+12345678901234"; // 15 digits

        // Act
        var phone = PhoneNumber.FromOptional(phoneString);

        // Assert
        phone.Should().NotBeNull();
        phone!.Value.Should().Be("+12345678901234");
    }

    [Fact]
    public void FromOptional_WithWhitespace_ShouldTrimAndClean()
    {
        // Arrange
        var phoneString = "  +1 555 123 4567  ";

        // Act
        var phone = PhoneNumber.FromOptional(phoneString);

        // Assert
        phone.Should().NotBeNull();
        phone!.Value.Should().Be("+15551234567");
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid Creation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void FromOptional_WithTooShortPhone_ShouldThrowException()
    {
        // Arrange
        var shortPhone = "+12345678"; // 9 digits

        // Act
        var act = () => PhoneNumber.FromOptional(shortPhone);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid phone number length*");
    }

    [Fact]
    public void FromOptional_WithTooLongPhone_ShouldThrowException()
    {
        // Arrange
        var longPhone = "+1234567890123456"; // 16 digits

        // Act
        var act = () => PhoneNumber.FromOptional(longPhone);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid phone number length*");
    }

    // ──────────────────────────────────────────────────────────────
    // Optional Handling
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FromOptional_WithEmptyOrNull_ShouldReturnNull(string? emptyPhone)
    {
        // Act
        var phone = PhoneNumber.FromOptional(emptyPhone);

        // Assert
        phone.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSamePhone_ShouldBeEqual()
    {
        // Arrange
        var phone1 = PhoneNumber.FromOptional("+1234567890");
        var phone2 = PhoneNumber.FromOptional("+1234567890");

        // Act & Assert
        phone1.Should().Be(phone2);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var phone = PhoneNumber.FromOptional("+1234567890");

        // Act
        var result = phone!.ToString();

        // Assert
        result.Should().Be("+1234567890");
    }
}
