using FluentAssertions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetInteractionTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InteractionType.Impression)]
    [InlineData(InteractionType.Swipe)]
    [InlineData(InteractionType.Rejection)]
    public void Create_WithValidData_ShouldCreateInteraction(InteractionType type)
    {
        // Arrange
        var petId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var interaction = PetInteraction.Create(petId, userId, type);

        // Assert
        interaction.Id.Should().NotBeEmpty();
        interaction.PetId.Should().Be(petId);
        interaction.UserId.Should().Be(userId);
        interaction.Type.Should().Be(type);
        interaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetInteraction.Create(Guid.Empty, Guid.NewGuid(), InteractionType.Impression);
        act.Should().Throw<ArgumentException>().WithParameterName("petId");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => PetInteraction.Create(Guid.NewGuid(), Guid.Empty, InteractionType.Impression);
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }
}
