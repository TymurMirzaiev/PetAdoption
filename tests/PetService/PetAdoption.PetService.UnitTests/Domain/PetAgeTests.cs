namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class PetAgeTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidAge_ShouldCreateInstance()
    {
        // Arrange & Act
        var age = new PetAge(24);

        // Assert
        age.Months.Should().Be(24);
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreateInstance()
    {
        // Arrange & Act
        var age = new PetAge(0);

        // Assert
        age.Months.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeAge_ShouldThrow()
    {
        // Act & Assert
        var act = () => new PetAge(-1);
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var age1 = new PetAge(12);
        var age2 = new PetAge(12);

        // Act & Assert
        age1.Should().Be(age2);
    }
}
