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

    // ──────────────────────────────────────────────────────────────
    // Approve
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_WhenPending_ShouldChangeStatusToApproved()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Approve();

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Approved);
        request.ReviewedAt.Should().NotBeNull();
        request.ReviewedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(AdoptionRequestStatus.Approved)]
    [InlineData(AdoptionRequestStatus.Rejected)]
    [InlineData(AdoptionRequestStatus.Cancelled)]
    public void Approve_WhenNotPending_ShouldThrow(AdoptionRequestStatus initialStatus)
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        TransitionTo(request, initialStatus);

        // Act & Assert
        var act = () => request.Approve();
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Reject
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_WhenPending_ShouldChangeStatusToRejected()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Reject("Pet already promised to another family.");

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Rejected);
        request.RejectionReason.Should().Be("Pet already promised to another family.");
        request.ReviewedAt.Should().NotBeNull();
        request.ReviewedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Reject_WithEmptyReason_ShouldThrow(string? reason)
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var act = () => request.Reject(reason!);
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Cancel
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.Cancel();

        // Assert
        request.Status.Should().Be(AdoptionRequestStatus.Cancelled);
        request.ReviewedAt.Should().NotBeNull();
        request.ReviewedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Cancel_WhenApproved_ShouldThrow()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Approve();

        // Act & Assert
        var act = () => request.Cancel();
        act.Should().Throw<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Domain Events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldRaiseAdoptionRequestCreatedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // Act
        var request = AdoptionRequest.Create(userId, petId, orgId);

        // Assert
        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionRequestCreatedEvent>()
            .Which.Should().Match<AdoptionRequestCreatedEvent>(e =>
                e.AggregateId == request.Id &&
                e.UserId == userId &&
                e.PetId == petId &&
                e.OrganizationId == orgId);
    }

    [Fact]
    public void Approve_ShouldRaiseAdoptionRequestApprovedEvent()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.ClearDomainEvents();

        // Act
        request.Approve();

        // Assert
        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionRequestApprovedEvent>();
    }

    [Fact]
    public void Reject_ShouldRaiseAdoptionRequestRejectedEventWithReason()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.ClearDomainEvents();

        // Act
        request.Reject("Not a fit");

        // Assert
        var raised = request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionRequestRejectedEvent>().Subject;
        raised.Reason.Should().Be("Not a fit");
    }

    [Fact]
    public void Cancel_ShouldRaiseAdoptionRequestCancelledEvent()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.ClearDomainEvents();

        // Act
        request.Cancel();

        // Assert
        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionRequestCancelledEvent>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheList()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        request.ClearDomainEvents();

        // Assert
        request.DomainEvents.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static void TransitionTo(AdoptionRequest request, AdoptionRequestStatus status)
    {
        switch (status)
        {
            case AdoptionRequestStatus.Approved:
                request.Approve();
                break;
            case AdoptionRequestStatus.Rejected:
                request.Reject("Test rejection reason");
                break;
            case AdoptionRequestStatus.Cancelled:
                request.Cancel();
                break;
        }
    }
}
