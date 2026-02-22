namespace PetAdoption.UserService.Tests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        // Arrange
        var emailString = "test@example.com";

        // Act
        var email = Email.From(emailString);

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_WithUpperCaseEmail_ShouldConvertToLowerCase()
    {
        // Arrange
        var emailString = "TEST@EXAMPLE.COM";

        // Act
        var email = Email.From(emailString);

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullEmail_ShouldThrowException(string? invalidEmail)
    {
        // Act
        var act = () => Email.From(invalidEmail!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be null or whitespace.*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("no@at.symbol")]
    public void Create_WithInvalidFormat_ShouldThrowException(string invalidEmail)
    {
        // Act
        var act = () => Email.From(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid email format.*");
    }

    [Fact]
    public void Create_WithEmailTooLong_ShouldThrowException()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // 262 characters

        // Act
        var act = () => Email.From(longEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot exceed 255 characters.*");
    }

    [Fact]
    public void Equals_WithSameEmail_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.From("test@example.com");
        var email2 = Email.From("TEST@EXAMPLE.COM");

        // Act & Assert
        email1.Should().Be(email2);
    }
}
