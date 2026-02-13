using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTypeTests
{
    [Theory]
    [InlineData("Dog")]
    [InlineData("Cat")]
    [InlineData("Rabbit")]
    [InlineData("Bird")]
    [InlineData("Fish")]
    [InlineData("Hamster")]
    public void Constructor_WithValidType_ShouldCreatePetType(string validType)
    {
        // Act
        var petType = new PetType(validType);

        // Assert
        petType.Value.Should().Be(validType);
    }

    [Theory]
    [InlineData("dog", "Dog")]
    [InlineData("DOG", "Dog")]
    [InlineData("dOg", "Dog")]
    [InlineData("cat", "Cat")]
    [InlineData("CAT", "Cat")]
    [InlineData("rabbit", "Rabbit")]
    [InlineData("RABBIT", "Rabbit")]
    [InlineData("bird", "Bird")]
    [InlineData("fish", "Fish")]
    [InlineData("hamster", "Hamster")]
    public void Constructor_WithCaseInsensitiveType_ShouldNormalizeToCanonicalForm(string input, string expected)
    {
        // Act
        var petType = new PetType(input);

        // Assert
        petType.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("  Dog  ", "Dog")]
    [InlineData("\tCat\t", "Cat")]
    [InlineData("\nRabbit\n", "Rabbit")]
    [InlineData("  fish  ", "Fish")]
    public void Constructor_WithWhitespaceAroundType_ShouldTrimAndNormalize(string input, string expected)
    {
        // Act
        var petType = new PetType(input);

        // Assert
        petType.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithNullOrWhitespaceType_ShouldThrowDomainException(string? invalidType)
    {
        // Act
        var act = () => new PetType(invalidType!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Theory]
    [InlineData("Snake")]
    [InlineData("Lizard")]
    [InlineData("Parrot")]
    [InlineData("Tiger")]
    [InlineData("InvalidType")]
    [InlineData("123")]
    public void Constructor_WithInvalidType_ShouldThrowDomainException(string invalidType)
    {
        // Act
        var act = () => new PetType(invalidType);

        // Assert
        var exception = act.Should().Throw<DomainException>().Which;
        exception.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
        exception.Message.Should().Contain("Dog, Cat, Rabbit, Bird, Fish, Hamster");
    }

    [Theory]
    [InlineData("Dog", "Dog", true)]
    [InlineData("dog", "DOG", true)] // Case-insensitive equality
    [InlineData("Cat", "cat", true)]
    [InlineData("Dog", "Cat", false)]
    [InlineData("Rabbit", "Bird", false)]
    public void Equals_ShouldCompareCaseInsensitively(string type1, string type2, bool expectedEqual)
    {
        // Arrange
        var petType1 = new PetType(type1);
        var petType2 = new PetType(type2);

        // Act & Assert
        petType1.Equals(petType2).Should().Be(expectedEqual);
        (petType1 == petType2).Should().Be(expectedEqual);
        (petType1 != petType2).Should().Be(!expectedEqual);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var petType = new PetType("Dog");

        // Act & Assert
        petType.Equals(null).Should().BeFalse();
        (petType == null).Should().BeFalse();
        (petType != null).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var petType = new PetType("Dog");

        // Act & Assert
        petType.Equals(petType).Should().BeTrue();
        (petType == petType).Should().BeTrue();
    }

    [Theory]
    [InlineData("Dog", "dog")]
    [InlineData("CAT", "cat")]
    [InlineData("Rabbit", "RABBIT")]
    public void GetHashCode_ForCaseInsensitiveEqualValues_ShouldBeSame(string type1, string type2)
    {
        // Arrange
        var petType1 = new PetType(type1);
        var petType2 = new PetType(type2);

        // Act & Assert
        petType1.GetHashCode().Should().Be(petType2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnCanonicalValue()
    {
        // Arrange
        var petType = new PetType("dog"); // Lowercase input

        // Act
        var result = petType.ToString();

        // Assert
        result.Should().Be("Dog"); // Canonical form
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        // Arrange
        var petType = new PetType("Cat");

        // Act
        string result = petType;

        // Assert
        result.Should().Be("Cat");
    }

    [Fact]
    public void ExplicitConversionFromString_ShouldCreatePetType()
    {
        // Arrange
        var type = "Rabbit";

        // Act
        var petType = (PetType)type;

        // Assert
        petType.Value.Should().Be("Rabbit");
    }

    [Fact]
    public void ExplicitConversionFromInvalidString_ShouldThrowDomainException()
    {
        // Arrange
        var invalidType = "Snake";

        // Act
        var act = () => (PetType)invalidType;

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidPetType);
    }

    [Fact]
    public void StaticProperties_ShouldProvidePreDefinedTypes()
    {
        // Act & Assert
        PetType.Dog.Value.Should().Be("Dog");
        PetType.Cat.Value.Should().Be("Cat");
        PetType.Rabbit.Value.Should().Be("Rabbit");
        PetType.Bird.Value.Should().Be("Bird");
        PetType.Fish.Value.Should().Be("Fish");
        PetType.Hamster.Value.Should().Be("Hamster");
    }

    [Fact]
    public void StaticProperties_ShouldBeEqualToNewInstancesWithSameValue()
    {
        // Arrange
        var dogInstance = new PetType("Dog");

        // Act & Assert
        dogInstance.Should().Be(PetType.Dog);
        dogInstance.GetHashCode().Should().Be(PetType.Dog.GetHashCode());
    }
}
