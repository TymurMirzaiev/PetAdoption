namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain.ValueObjects;
using PetAdoption.PetService.Domain.Exceptions;

public class AnnouncementTitleTests
{
    // ──────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidTitle_ShouldCreateInstance()
    {
        // Arrange & Act
        var title = new AnnouncementTitle("Holiday Hours");

        // Assert
        title.Value.Should().Be("Holiday Hours");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => new AnnouncementTitle(value!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_ExceedingMaxLength_ShouldThrow()
    {
        // Act & Assert
        var act = () => new AnnouncementTitle(new string('a', 201));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        // Arrange & Act
        var title = new AnnouncementTitle("  Hello  ");

        // Assert
        title.Value.Should().Be("Hello");
    }
}
