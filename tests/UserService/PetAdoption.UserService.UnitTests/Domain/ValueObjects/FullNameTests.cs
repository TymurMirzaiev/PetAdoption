namespace PetAdoption.UserService.UnitTests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class FullNameTests
{
    // ──────────────────────────────────────────────────────────────
    // Valid Creation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        // Arrange
        var nameString = "John Doe";

        // Act
        var fullName = FullName.From(nameString);

        // Assert
        fullName.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Create_WithMinimumLength_ShouldSucceed()
    {
        // Arrange
        var nameString = "AB";

        // Act
        var fullName = FullName.From(nameString);

        // Assert
        fullName.Value.Should().Be("AB");
    }

    [Fact]
    public void Create_WithMaximumLength_ShouldSucceed()
    {
        // Arrange
        var nameString = new string('A', 100);

        // Act
        var fullName = FullName.From(nameString);

        // Assert
        fullName.Value.Should().Be(nameString);
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid Creation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullName_ShouldThrowException(string? invalidName)
    {
        // Act
        var act = () => FullName.From(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty*");
    }

    [Fact]
    public void Create_WithNameTooShort_ShouldThrowException()
    {
        // Arrange
        var shortName = "A";

        // Act
        var act = () => FullName.From(shortName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name must be at least 2 characters*");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldThrowException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        var act = () => FullName.From(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot exceed 100 characters*");
    }

    // ──────────────────────────────────────────────────────────────
    // Trimming
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithNameHavingWhitespace_ShouldTrim()
    {
        // Arrange
        var nameString = "  John Doe  ";

        // Act
        var fullName = FullName.From(nameString);

        // Assert
        fullName.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Equals_WithSameName_ShouldBeEqual()
    {
        // Arrange
        var name1 = FullName.From("John Doe");
        var name2 = FullName.From("  John Doe  ");

        // Act & Assert
        name1.Should().Be(name2);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var fullName = FullName.From("John Doe");

        // Act
        var result = fullName.ToString();

        // Assert
        result.Should().Be("John Doe");
    }
}
