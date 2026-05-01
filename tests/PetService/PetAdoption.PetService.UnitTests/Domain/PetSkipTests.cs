namespace PetAdoption.PetService.UnitTests.Domain;

using FluentAssertions;
using PetAdoption.PetService.Domain;

public class PetSkipTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        // Act
        var skip = PetSkip.Create(userId, petId);

        // Assert
        skip.Id.Should().NotBeEmpty();
        skip.UserId.Should().Be(userId);
        skip.PetId.Should().Be(petId);
        skip.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetSkip.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetSkip.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
