using FluentAssertions;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTagsOnPetTests
{
    private static readonly Guid TestPetTypeId = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────
    // Create with Tags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithTags_ShouldSetTags()
    {
        // Arrange
        var tags = new[] { "friendly", "vaccinated" };

        // Act
        var pet = Pet.Create("Bella", TestPetTypeId, tags: tags);

        // Assert
        pet.Tags.Should().HaveCount(2);
        pet.Tags.Select(t => t.Value).Should().Contain("friendly");
        pet.Tags.Select(t => t.Value).Should().Contain("vaccinated");
    }

    [Fact]
    public void Create_WithNullTags_ShouldHaveEmptyTags()
    {
        // Act
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDuplicateTags_ShouldDedup()
    {
        // Arrange
        var tags = new[] { "friendly", "Friendly", "FRIENDLY" };

        // Act
        var pet = Pet.Create("Bella", TestPetTypeId, tags: tags);

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("friendly");
    }

    // ──────────────────────────────────────────────────────────────
    // AddTag
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddTag_WithNewTag_ShouldAddTag()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Act
        pet.AddTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("friendly");
    }

    [Fact]
    public void AddTag_WithDuplicateTag_ShouldNotAddDuplicate()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.AddTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void AddTag_CaseInsensitiveDuplicate_ShouldNotAdd()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.AddTag("FRIENDLY");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // RemoveTag
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveTag_WithExistingTag_ShouldRemove()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly", "vaccinated" });

        // Act
        pet.RemoveTag("friendly");

        // Assert
        pet.Tags.Should().HaveCount(1);
        pet.Tags.First().Value.Should().Be("vaccinated");
    }

    [Fact]
    public void RemoveTag_CaseInsensitive_ShouldRemove()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.RemoveTag("FRIENDLY");

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_NonExistentTag_ShouldDoNothing()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.RemoveTag("vaccinated");

        // Assert
        pet.Tags.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // SetTags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetTags_ShouldReplaceAllTags()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly", "vaccinated" });

        // Act
        pet.SetTags(new[] { "neutered", "house-trained" });

        // Assert
        pet.Tags.Should().HaveCount(2);
        pet.Tags.Select(t => t.Value).Should().BeEquivalentTo(new[] { "neutered", "house-trained" });
    }

    [Fact]
    public void SetTags_WithEmptyList_ShouldClearTags()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId, tags: new[] { "friendly" });

        // Act
        pet.SetTags(Enumerable.Empty<string>());

        // Assert
        pet.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SetTags_WithDuplicates_ShouldDedup()
    {
        // Arrange
        var pet = Pet.Create("Bella", TestPetTypeId);

        // Act
        pet.SetTags(new[] { "friendly", "Friendly" });

        // Assert
        pet.Tags.Should().HaveCount(1);
    }
}
