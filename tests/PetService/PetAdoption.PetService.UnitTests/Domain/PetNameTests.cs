using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetNameTests
{
    [Theory]
    [InlineData("Bella")]
    [InlineData("Max")]
    [InlineData("Charlie Brown")]
    [InlineData("A")] // Min length = 1
    [InlineData("ThisIsAVeryLongPetNameThatIsStillValidBecauseItIsExactly100CharactersLongAndThatIsTheMaximumOk")] // 100 chars
    public void Constructor_WithValidName_ShouldCreatePetName(string validName)
    {
        // Act
        var petName = new PetName(validName);

        // Assert
        petName.Value.Should().Be(validName.Trim());
    }

    [Theory]
    [InlineData("  Bella  ", "Bella")]
    [InlineData("\tMax\t", "Max")]
    [InlineData("\nCharlie\n", "Charlie")]
    [InlineData("  Fluffy  ", "Fluffy")]
    public void Constructor_WithWhitespaceAroundName_ShouldTrimName(string input, string expected)
    {
        // Act
        var petName = new PetName(input);

        // Assert
        petName.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithNullOrWhitespaceName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        var act = () => new PetName(invalidName!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetName);
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_ShouldThrowDomainException()
    {
        // Arrange - Create a string with 101 characters (exceeds max of 100)
        var tooLongName = new string('A', PetName.MaxLength + 1);

        // Act
        var act = () => new PetName(tooLongName);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetName);
    }

    [Theory]
    [InlineData("Bella", "Bella", true)]
    [InlineData("Max", "Max", true)]
    [InlineData("Bella", "Max", false)]
    [InlineData("bella", "BELLA", false)] // Case-sensitive
    public void Equals_ShouldCompareByValue(string name1, string name2, bool expectedEqual)
    {
        // Arrange
        var petName1 = new PetName(name1);
        var petName2 = new PetName(name2);

        // Act & Assert
        petName1.Equals(petName2).Should().Be(expectedEqual);
        (petName1 == petName2).Should().Be(expectedEqual);
        (petName1 != petName2).Should().Be(!expectedEqual);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var petName = new PetName("Bella");

        // Act & Assert
        petName.Equals(null).Should().BeFalse();
        (petName == null).Should().BeFalse();
        (petName != null).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var petName = new PetName("Bella");

        // Act & Assert
        petName.Equals(petName).Should().BeTrue();
        (petName == petName).Should().BeTrue();
    }

    [Theory]
    [InlineData("Bella", "Bella")]
    [InlineData("Max", "Max")]
    public void GetHashCode_ForEqualValues_ShouldBeSame(string name1, string name2)
    {
        // Arrange
        var petName1 = new PetName(name1);
        var petName2 = new PetName(name2);

        // Act & Assert
        petName1.GetHashCode().Should().Be(petName2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var petName = new PetName("Bella");

        // Act
        var result = petName.ToString();

        // Assert
        result.Should().Be("Bella");
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        // Arrange
        var petName = new PetName("Max");

        // Act
        string result = petName;

        // Assert
        result.Should().Be("Max");
    }

    [Fact]
    public void ExplicitConversionFromString_ShouldCreatePetName()
    {
        // Arrange
        var name = "Charlie";

        // Act
        var petName = (PetName)name;

        // Assert
        petName.Value.Should().Be("Charlie");
    }

    [Fact]
    public void ExplicitConversionFromInvalidString_ShouldThrowDomainException()
    {
        // Arrange
        var invalidName = "";

        // Act
        var act = () => (PetName)invalidName;

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetName);
    }
}
