namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

public class PetBreedTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidBreed_ShouldCreateInstance()
    {
        // Arrange
        var breed = "Golden Retriever";

        // Act
        var result = new PetBreed(breed);

        // Assert
        result.Value.Should().Be("Golden Retriever");
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var result = new PetBreed("  Labrador  ");

        // Assert
        result.Value.Should().Be("Labrador");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? breed)
    {
        // Act & Assert
        var act = () => new PetBreed(breed!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithTooLongBreed_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetBreed(new string('a', 101));
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var breed1 = new PetBreed("Siamese");
        var breed2 = new PetBreed("Siamese");

        // Act & Assert
        breed1.Should().Be(breed2);
    }

    // ──────────────────────────────────────────────────────────────
    // Conversion
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var breed = new PetBreed("Poodle");

        // Act
        string result = breed;

        // Assert
        result.Should().Be("Poodle");
    }
}
