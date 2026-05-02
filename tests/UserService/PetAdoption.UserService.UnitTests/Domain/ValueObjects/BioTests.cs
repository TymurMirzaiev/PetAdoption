namespace PetAdoption.UserService.Tests.Domain.ValueObjects;

using FluentAssertions;
using PetAdoption.UserService.Domain.ValueObjects;

public class BioTests
{
    // ──────────────────────────────────────────────────────────────
    // FromOptional — null / empty / whitespace
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void FromOptional_EmptyInput_ReturnsNull()
    {
        // Act & Assert
        Bio.FromOptional("").Should().BeNull();
    }

    [Fact]
    public void FromOptional_WhitespaceInput_ReturnsNull()
    {
        // Act & Assert
        Bio.FromOptional("   ").Should().BeNull();
    }

    [Fact]
    public void FromOptional_NullInput_ReturnsNull()
    {
        // Act & Assert
        Bio.FromOptional(null).Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // FromOptional — valid input
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void FromOptional_ValidInput_ReturnsBio()
    {
        // Arrange
        var raw = "I love animals and have a big yard.";

        // Act
        var bio = Bio.FromOptional(raw);

        // Assert
        bio.Should().NotBeNull();
        bio!.Value.Should().Be(raw);
    }

    [Fact]
    public void FromOptional_TrimsWhitespace()
    {
        // Arrange
        var raw = "  some bio text  ";

        // Act
        var bio = Bio.FromOptional(raw);

        // Assert
        bio.Should().NotBeNull();
        bio!.Value.Should().Be("some bio text");
    }

    [Fact]
    public void FromOptional_Exactly1000Chars_ReturnsBio()
    {
        // Arrange
        var raw = new string('a', 1000);

        // Act
        var bio = Bio.FromOptional(raw);

        // Assert
        bio.Should().NotBeNull();
        bio!.Value.Should().HaveLength(1000);
    }

    // ──────────────────────────────────────────────────────────────
    // FromOptional — over limit
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void FromOptional_Over1000Chars_ThrowsArgumentException()
    {
        // Arrange
        var raw = new string('a', 1001);

        // Act
        var act = () => Bio.FromOptional(raw);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Bio must not exceed 1000 characters.*");
    }

    // ──────────────────────────────────────────────────────────────
    // ToString
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var bio = Bio.FromOptional("hello");

        // Act
        var result = bio!.ToString();

        // Assert
        result.Should().Be("hello");
    }
}
