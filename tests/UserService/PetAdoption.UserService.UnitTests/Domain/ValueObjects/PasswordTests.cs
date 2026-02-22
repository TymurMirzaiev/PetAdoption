namespace PetAdoption.UserService.Tests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void FromHash_WithValidHash_ShouldSucceed()
    {
        // Arrange
        var hashedPassword = "$2a$12$somehashvalue1234567890";

        // Act
        var password = Password.FromHash(hashedPassword);

        // Assert
        password.HashedValue.Should().Be(hashedPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FromHash_WithEmptyOrNull_ShouldThrowException(string? invalidHash)
    {
        // Act
        var act = () => Password.FromHash(invalidHash!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash cannot be empty*");
    }

    [Fact]
    public void ValidatePlainText_WithValidPassword_ShouldNotThrow()
    {
        // Arrange
        var plainPassword = "SecurePass123!";

        // Act
        var act = () => Password.ValidatePlainText(plainPassword);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidatePlainText_WithEmptyOrNull_ShouldThrowException(string? invalidPassword)
    {
        // Act
        var act = () => Password.ValidatePlainText(invalidPassword!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be empty*");
    }

    [Fact]
    public void ValidatePlainText_WithTooShortPassword_ShouldThrowException()
    {
        // Arrange
        var shortPassword = "Pass1";

        // Act
        var act = () => Password.ValidatePlainText(shortPassword);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password must be at least 8 characters*");
    }

    [Fact]
    public void ValidatePlainText_WithTooLongPassword_ShouldThrowException()
    {
        // Arrange
        var longPassword = new string('a', 101);

        // Act
        var act = () => Password.ValidatePlainText(longPassword);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot exceed 100 characters*");
    }

    [Fact]
    public void ValidatePlainText_WithMinimumLength_ShouldNotThrow()
    {
        // Arrange
        var password = "Password";

        // Act
        var act = () => Password.ValidatePlainText(password);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePlainText_WithMaximumLength_ShouldNotThrow()
    {
        // Arrange
        var password = new string('a', 100);

        // Act
        var act = () => Password.ValidatePlainText(password);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Equals_WithSameHash_ShouldBeEqual()
    {
        // Arrange
        var hash = "$2a$12$somehashvalue1234567890";
        var password1 = Password.FromHash(hash);
        var password2 = Password.FromHash(hash);

        // Act & Assert
        password1.Should().Be(password2);
    }
}
