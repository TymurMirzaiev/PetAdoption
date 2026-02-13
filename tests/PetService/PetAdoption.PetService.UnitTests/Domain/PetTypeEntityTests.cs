using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTypeEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateActivePetType()
    {
        // Arrange
        var code = "dragon";
        var name = "Dragon";

        // Act
        var petType = PetType.Create(code, name);

        // Assert
        petType.Should().NotBeNull();
        petType.Id.Should().NotBeEmpty();
        petType.Code.Should().Be("dragon"); // Normalized to lowercase
        petType.Name.Should().Be("Dragon");
        petType.IsActive.Should().BeTrue();
        petType.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        petType.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeCodeToLowercase()
    {
        // Arrange & Act
        var petType = PetType.Create("DRAGON", "Dragon");

        // Assert
        petType.Code.Should().Be("dragon");
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrowDomainException()
    {
        // Act
        var act = () => PetType.Create("", "Dragon");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        // Act
        var act = () => PetType.Create("dragon", "");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void Create_WithCodeTooShort_ShouldThrowDomainException()
    {
        // Act
        var act = () => PetType.Create("d", "Dragon");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void Create_WithCodeTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longCode = new string('a', 51);

        // Act
        var act = () => PetType.Create(longCode, "Dragon");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateNameAndTimestamp()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");
        var originalCreatedAt = petType.CreatedAt;

        // Act
        petType.UpdateName("Fire Dragon");

        // Assert
        petType.Name.Should().Be("Fire Dragon");
        petType.UpdatedAt.Should().NotBeNull();
        petType.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        petType.CreatedAt.Should().Be(originalCreatedAt); // CreatedAt unchanged
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowDomainException()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");

        // Act
        var act = () => petType.UpdateName("");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");

        // Act
        petType.Deactivate();

        // Assert
        petType.IsActive.Should().BeFalse();
        petType.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowDomainException()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");
        petType.Deactivate();

        // Act
        var act = () => petType.Deactivate();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOperation);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");
        petType.Deactivate();

        // Act
        petType.Activate();

        // Assert
        petType.IsActive.Should().BeTrue();
        petType.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowDomainException()
    {
        // Arrange
        var petType = PetType.Create("dragon", "Dragon");

        // Act
        var act = () => petType.Activate();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOperation);
    }

    [Fact]
    public void Lifecycle_CreateUpdateDeactivateActivate_ShouldWorkCorrectly()
    {
        // Create
        var petType = PetType.Create("dragon", "Dragon");
        petType.IsActive.Should().BeTrue();

        // Update
        petType.UpdateName("Fire Dragon");
        petType.Name.Should().Be("Fire Dragon");

        // Deactivate
        petType.Deactivate();
        petType.IsActive.Should().BeFalse();

        // Activate again
        petType.Activate();
        petType.IsActive.Should().BeTrue();
    }
}
