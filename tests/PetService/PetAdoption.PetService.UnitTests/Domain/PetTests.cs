using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateAvailablePet()
    {
        // Arrange
        var name = "Bella";
        var type = "Dog";

        // Act
        var pet = Pet.Create(name, type);

        // Assert
        pet.Should().NotBeNull();
        pet.Id.Should().NotBeEmpty();
        pet.Name.Value.Should().Be(name);
        pet.Type.Value.Should().Be(type);
        pet.Status.Should().Be(PetStatus.Available);
        pet.Version.Should().Be(0);
        pet.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reserve_WhenPetIsAvailable_ShouldChangeStatusToReserved()
    {
        // Arrange
        var pet = Pet.Create("Bella", "Dog");

        // Act
        pet.Reserve();

        // Assert
        pet.Status.Should().Be(PetStatus.Reserved);
        pet.DomainEvents.Should().HaveCount(1);
        pet.DomainEvents.First().Should().BeOfType<PetReservedEvent>();

        var domainEvent = (PetReservedEvent)pet.DomainEvents.First();
        domainEvent.AggregateId.Should().Be(pet.Id);
        domainEvent.PetName.Should().Be("Bella");
    }

    [Fact]
    public void Reserve_WhenPetIsReserved_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Bella", "Dog");
        pet.Reserve();

        // Act
        var act = () => pet.Reserve();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotAvailable);
    }

    [Fact]
    public void Reserve_WhenPetIsAdopted_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Bella", "Dog");
        pet.Reserve();
        pet.Adopt();

        // Act
        var act = () => pet.Reserve();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotAvailable);
    }

    [Fact]
    public void Adopt_WhenPetIsReserved_ShouldChangeStatusToAdopted()
    {
        // Arrange
        var pet = Pet.Create("Max", "Cat");
        pet.Reserve();
        pet.ClearDomainEvents(); // Clear reservation event

        // Act
        pet.Adopt();

        // Assert
        pet.Status.Should().Be(PetStatus.Adopted);
        pet.DomainEvents.Should().HaveCount(1);
        pet.DomainEvents.First().Should().BeOfType<PetAdoptedEvent>();

        var domainEvent = (PetAdoptedEvent)pet.DomainEvents.First();
        domainEvent.AggregateId.Should().Be(pet.Id);
        domainEvent.PetName.Should().Be("Max");
    }

    [Fact]
    public void Adopt_WhenPetIsAvailable_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Max", "Cat");

        // Act
        var act = () => pet.Adopt();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotReserved);
    }

    [Fact]
    public void Adopt_WhenPetIsAdopted_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Max", "Cat");
        pet.Reserve();
        pet.Adopt();

        // Act
        var act = () => pet.Adopt();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotReserved);
    }

    [Fact]
    public void CancelReservation_WhenPetIsReserved_ShouldChangeStatusToAvailable()
    {
        // Arrange
        var pet = Pet.Create("Charlie", "Rabbit");
        pet.Reserve();
        pet.ClearDomainEvents(); // Clear reservation event

        // Act
        pet.CancelReservation();

        // Assert
        pet.Status.Should().Be(PetStatus.Available);
        pet.DomainEvents.Should().HaveCount(1);
        pet.DomainEvents.First().Should().BeOfType<PetReservationCancelledEvent>();

        var domainEvent = (PetReservationCancelledEvent)pet.DomainEvents.First();
        domainEvent.AggregateId.Should().Be(pet.Id);
        domainEvent.PetName.Should().Be("Charlie");
    }

    [Fact]
    public void CancelReservation_WhenPetIsAvailable_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Charlie", "Rabbit");

        // Act
        var act = () => pet.CancelReservation();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotReserved);
    }

    [Fact]
    public void CancelReservation_WhenPetIsAdopted_ShouldThrowDomainException()
    {
        // Arrange
        var pet = Pet.Create("Charlie", "Rabbit");
        pet.Reserve();
        pet.Adopt();

        // Act
        var act = () => pet.CancelReservation();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.PetNotReserved);
    }

    [Fact]
    public void CompleteWorkflow_ReserveAdopt_ShouldSucceed()
    {
        // Arrange
        var pet = Pet.Create("Buddy", "Dog");

        // Act & Assert - Reserve
        pet.Reserve();
        pet.Status.Should().Be(PetStatus.Reserved);
        pet.DomainEvents.Should().HaveCount(1);

        // Act & Assert - Adopt
        pet.Adopt();
        pet.Status.Should().Be(PetStatus.Adopted);
        pet.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void CompleteWorkflow_ReserveCancelReserve_ShouldSucceed()
    {
        // Arrange
        var pet = Pet.Create("Luna", "Cat");

        // Act & Assert - Reserve
        pet.Reserve();
        pet.Status.Should().Be(PetStatus.Reserved);

        // Act & Assert - Cancel
        pet.CancelReservation();
        pet.Status.Should().Be(PetStatus.Available);

        // Act & Assert - Reserve again
        pet.Reserve();
        pet.Status.Should().Be(PetStatus.Reserved);
        pet.DomainEvents.Should().HaveCount(3); // Reserve, Cancel, Reserve
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var pet = Pet.Create("Rocky", "Hamster");
        pet.Reserve();
        pet.DomainEvents.Should().HaveCount(1);

        // Act
        pet.ClearDomainEvents();

        // Assert
        pet.DomainEvents.Should().BeEmpty();
    }
}
