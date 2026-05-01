using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class AdoptionRequestTests
{
    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreatePendingRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act
        var request = AdoptionRequest.Create(userId, petId, organizationId, "I love dogs and have a big yard.");

        // Assert
        request.Id.Should().NotBeEmpty();
        request.UserId.Should().Be(userId);
        request.PetId.Should().Be(petId);
        request.OrganizationId.Should().Be(organizationId);
        request.Status.Should().Be(AdoptionRequestStatus.Pending);
        request.Message.Should().Be("I love dogs and have a big yard.");
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.ReviewedAt.Should().BeNull();
        request.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void Create_WithEmptyPetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("petId");
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        // Act & Assert
        var act = () => AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("organizationId");
    }

    [Fact]
    public void Create_WithNullMessage_ShouldSucceed()
    {
        // Act
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        request.Message.Should().BeNull();
    }
}
