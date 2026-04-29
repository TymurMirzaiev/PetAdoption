namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class AnnouncementBodyTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidBody_ShouldCreateInstance()
    {
        // Arrange & Act
        var body = new AnnouncementBody("We will be closed on Monday.");

        // Assert
        body.Value.Should().Be("We will be closed on Monday.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => new AnnouncementBody(value!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_ExceedingMaxLength_ShouldThrow()
    {
        // Act & Assert
        var act = () => new AnnouncementBody(new string('a', 5001));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var body = new AnnouncementBody("  Hello  ");

        // Assert
        body.Value.Should().Be("Hello");
    }
}
