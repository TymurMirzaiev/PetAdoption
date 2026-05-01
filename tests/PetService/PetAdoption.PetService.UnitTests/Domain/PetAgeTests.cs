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

    // ──────────────────────────────────────────────────────────────
    // Comparison
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void GreaterThan_WhenLeftIsBigger_ShouldBeTrue()
    {
        // Arrange
        var older = new PetAge(36);
        var younger = new PetAge(12);

        // Act & Assert
        (older > younger).Should().BeTrue();
        (younger > older).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanOrEqual_WhenEqualOrBigger_ShouldBeTrue()
    {
        // Arrange
        var a = new PetAge(24);
        var b = new PetAge(24);
        var c = new PetAge(12);

        // Act & Assert
        (a >= b).Should().BeTrue();
        (a >= c).Should().BeTrue();
        (c >= a).Should().BeFalse();
    }

    [Fact]
    public void LessThan_WhenLeftIsSmaller_ShouldBeTrue()
    {
        // Arrange
        var younger = new PetAge(6);
        var older = new PetAge(24);

        // Act & Assert
        (younger < older).Should().BeTrue();
        (older < younger).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual_WhenEqualOrSmaller_ShouldBeTrue()
    {
        // Arrange
        var a = new PetAge(12);
        var b = new PetAge(12);
        var c = new PetAge(24);

        // Act & Assert
        (a <= b).Should().BeTrue();
        (a <= c).Should().BeTrue();
        (c <= a).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Arrange
        var age = new PetAge(12);

        // Act & Assert
        age.CompareTo(null).Should().BeGreaterThan(0);
    }
}
