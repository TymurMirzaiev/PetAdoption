using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTagTests
{
    // ──────────────────────────────────────────────────────────────
    // Valid Creation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("friendly", "friendly")]
    [InlineData("Friendly", "friendly")]
    [InlineData("VACCINATED", "vaccinated")]
    [InlineData("house-trained", "house-trained")]
    [InlineData("good-with-kids", "good-with-kids")]
    [InlineData("  special-needs  ", "special-needs")]
    public void Constructor_WithValidTag_ShouldNormalizeToLowerTrimmed(string input, string expected)
    {
        // Act
        var tag = new PetTag(input);

        // Assert
        tag.Value.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithMaxLengthTag_ShouldSucceed()
    {
        // Arrange
        var maxTag = new string('a', PetTag.MaxLength);

        // Act
        var tag = new PetTag(maxTag);

        // Assert
        tag.Value.Should().Be(maxTag);
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid Creation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithNullOrWhitespace_ShouldThrowDomainException(string? invalidTag)
    {
        // Act
        var act = () => new PetTag(invalidTag!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetTag);
    }

    [Fact]
    public void Constructor_WithTagExceedingMaxLength_ShouldThrowDomainException()
    {
        // Arrange
        var tooLong = new string('a', PetTag.MaxLength + 1);

        // Act
        var act = () => new PetTag(tooLong);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetTag);
    }

    // ──────────────────────────────────────────────────────────────
    // Equality
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("Friendly");

        // Act & Assert
        tag1.Equals(tag2).Should().BeTrue();
        (tag1 == tag2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("vaccinated");

        // Act & Assert
        tag1.Equals(tag2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ForEqualValues_ShouldBeSame()
    {
        // Arrange
        var tag1 = new PetTag("friendly");
        var tag2 = new PetTag("FRIENDLY");

        // Act & Assert
        tag1.GetHashCode().Should().Be(tag2.GetHashCode());
    }

    // ──────────────────────────────────────────────────────────────
    // String Conversion
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var tag = new PetTag("friendly");

        // Act & Assert
        tag.ToString().Should().Be("friendly");
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        // Arrange
        var tag = new PetTag("vaccinated");

        // Act
        string result = tag;

        // Assert
        result.Should().Be("vaccinated");
    }
}
