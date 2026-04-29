namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

public class AnnouncementTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddDays(7);
        var createdBy = Guid.NewGuid();

        // Act
        var announcement = Announcement.Create("Title", "Body text", start, end, createdBy);

        // Assert
        announcement.Id.Should().NotBeEmpty();
        announcement.Title.Value.Should().Be("Title");
        announcement.Body.Value.Should().Be("Body text");
        announcement.StartDate.Should().Be(start);
        announcement.EndDate.Should().Be(end);
        announcement.CreatedBy.Should().Be(createdBy);
        announcement.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEndBeforeStart_ShouldThrow()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);

        // Act & Assert
        var act = () => Announcement.Create("Title", "Body", start, end, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEndEqualToStart_ShouldThrow()
    {
        // Arrange
        var date = DateTime.UtcNow;

        // Act & Assert
        var act = () => Announcement.Create("Title", "Body", date, date, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var announcement = Announcement.Create("Old", "Old body", start, start.AddDays(7), Guid.NewGuid());
        var newStart = DateTime.UtcNow.AddDays(1);
        var newEnd = DateTime.UtcNow.AddDays(14);

        // Act
        announcement.Update("New", "New body", newStart, newEnd);

        // Assert
        announcement.Title.Value.Should().Be("New");
        announcement.Body.Value.Should().Be("New body");
        announcement.StartDate.Should().Be(newStart);
        announcement.EndDate.Should().Be(newEnd);
    }

    [Fact]
    public void Update_WithEndBeforeStart_ShouldThrow()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var announcement = Announcement.Create("Title", "Body", start, start.AddDays(7), Guid.NewGuid());

        // Act & Assert
        var act = () => announcement.Update("New", "New body", start, start.AddDays(-1));
        act.Should().Throw<DomainException>();
    }
}
