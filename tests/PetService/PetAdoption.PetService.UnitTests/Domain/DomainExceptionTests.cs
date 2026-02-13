using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetProperties()
    {
        // Arrange
        var errorCode = PetDomainErrorCode.PetNotFound;
        var message = "Pet not found";

        // Act
        var exception = new DomainException(errorCode, message);

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.Message.Should().Be(message);
        exception.Metadata.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithErrorCodeMessageAndMetadata_ShouldSetAllProperties()
    {
        // Arrange
        var errorCode = PetDomainErrorCode.PetNotAvailable;
        var message = "Pet is not available for reservation";
        var metadata = new Dictionary<string, object>
        {
            { "PetId", Guid.NewGuid() },
            { "CurrentStatus", "Reserved" },
            { "RequiredStatus", "Available" }
        };

        // Act
        var exception = new DomainException(errorCode, message, metadata);

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.Message.Should().Be(message);
        exception.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Constructor_WithEmptyMetadata_ShouldSetMetadata()
    {
        // Arrange
        var errorCode = PetDomainErrorCode.InvalidPetName;
        var message = "Invalid pet name";
        var metadata = new Dictionary<string, object>();

        // Act
        var exception = new DomainException(errorCode, message, metadata);

        // Assert
        exception.Metadata.Should().NotBeNull();
        exception.Metadata.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PetDomainErrorCode.PetNotFound, "pet_not_found")]
    [InlineData(PetDomainErrorCode.PetNotAvailable, "pet_not_available")]
    [InlineData(PetDomainErrorCode.PetNotReserved, "pet_not_reserved")]
    [InlineData(PetDomainErrorCode.InvalidPetName, "invalid_pet_name")]
    [InlineData(PetDomainErrorCode.InvalidPetType, "invalid_pet_type")]
    [InlineData(PetDomainErrorCode.ConcurrencyConflict, "concurrency_conflict")]
    public void ErrorCode_ShouldMatchProvidedValue(string errorCode, string expectedValue)
    {
        // Act
        var exception = new DomainException(errorCode, "Test message");

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.ErrorCode.Should().Be(expectedValue);
    }

    [Fact]
    public void Constructor_WithNullErrorCode_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DomainException(null!, "Test message");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorCode");
    }

    [Fact]
    public void DomainException_ShouldBeException()
    {
        // Arrange
        var exception = new DomainException(PetDomainErrorCode.PetNotFound, "Not found");

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ErrorCodeConstants_ShouldUseSnakeCaseFormat()
    {
        // Act & Assert - Verify all error codes follow snake_case convention
        PetDomainErrorCode.PetNotFound.Should().Be("pet_not_found");
        PetDomainErrorCode.PetNotAvailable.Should().Be("pet_not_available");
        PetDomainErrorCode.PetNotReserved.Should().Be("pet_not_reserved");
        PetDomainErrorCode.InvalidPetName.Should().Be("invalid_pet_name");
        PetDomainErrorCode.InvalidPetType.Should().Be("invalid_pet_type");
        PetDomainErrorCode.ConcurrencyConflict.Should().Be("concurrency_conflict");
        PetDomainErrorCode.InvalidOperation.Should().Be("invalid_operation");
        PetDomainErrorCode.UnknownDomainError.Should().Be("unknown_domain_error");
    }

    [Fact]
    public void ErrorCodeConstants_ShouldAllBeDefined()
    {
        // This test ensures all error codes mentioned in documentation are defined
        // Act & Assert
        PetDomainErrorCode.PetNotFound.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.PetNotAvailable.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.PetNotReserved.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.InvalidPetName.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.InvalidPetType.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.ConcurrencyConflict.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.InvalidOperation.Should().NotBeNullOrWhiteSpace();
        PetDomainErrorCode.UnknownDomainError.Should().NotBeNullOrWhiteSpace();
    }
}
