namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class PetDescriptionTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidDescription_ShouldCreateInstance()
    {
        // Arrange & Act
        var desc = new PetDescription("A friendly dog.");

        // Assert
        desc.Value.Should().Be("A friendly dog.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => new PetDescription(value!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithTooLongDescription_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetDescription(new string('a', 2001));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var desc = new PetDescription("  Hello  ");

        // Assert
        desc.Value.Should().Be("Hello");
    }
}
