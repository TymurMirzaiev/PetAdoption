using FluentAssertions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.UnitTests.Domain;

public class OrganizationTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreateActiveOrganization()
    {
        // Act
        var org = Organization.Create("Happy Paws", "happy-paws", "A shelter");

        // Assert
        org.Id.Should().NotBeEmpty();
        org.Name.Should().Be("Happy Paws");
        org.Slug.Should().Be("happy-paws");
        org.Description.Should().Be("A shelter");
        org.Status.Should().Be(OrganizationStatus.Active);
        org.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithDeterministicId_ShouldUseProvidedId()
    {
        // Arrange
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var org = Organization.Create(id, "Happy Paws", "happy-paws", "A shelter");

        // Assert
        org.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void Create_WithInvalidName_ShouldThrow(string name)
    {
        // Act & Assert
        var act = () => Organization.Create(name, "valid-slug", null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("has spaces")]
    [InlineData("special!chars")]
    public void Create_WithInvalidSlug_ShouldThrow(string slug)
    {
        // Act & Assert
        var act = () => Organization.Create("Valid Name", slug, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithValidSlug_ShouldAccept()
    {
        // Act
        var org = Organization.Create("Test", "valid-slug-123", null);

        // Assert
        org.Slug.Should().Be("valid-slug-123");
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var org = Organization.Create("Old Name", "slug", "Old desc");

        // Act
        org.Update("New Name", "New desc");

        // Assert
        org.Name.Should().Be("New Name");
        org.Description.Should().Be("New desc");
        org.UpdatedAt.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // Deactivate / Activate
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_ShouldSetInactive()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);

        // Act
        org.Deactivate();

        // Assert
        org.Status.Should().Be(OrganizationStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrow()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);
        org.Deactivate();

        // Act & Assert
        var act = () => org.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetActive()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);
        org.Deactivate();

        // Act
        org.Activate();

        // Assert
        org.Status.Should().Be(OrganizationStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange
        var org = Organization.Create("Test", "test-slug", null);

        // Act & Assert
        var act = () => org.Activate();
        act.Should().Throw<InvalidOperationException>();
    }
}
