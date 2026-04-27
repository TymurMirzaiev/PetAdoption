namespace PetAdoption.UserService.Tests.Application.Queries;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Queries;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class GetUserByEmailQueryHandlerTests
{
    private readonly Mock<IUserQueryStore> _mockUserQueryStore;
    private readonly GetUserByEmailQueryHandler _handler;

    public GetUserByEmailQueryHandlerTests()
    {
        _mockUserQueryStore = new Mock<IUserQueryStore>();
        _handler = new GetUserByEmailQueryHandler(_mockUserQueryStore.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success Cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithExistingEmail_ShouldReturnUserDto()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            "+1234567890"
        );

        var query = new GetUserByEmailQuery("test@example.com");

        _mockUserQueryStore
            .Setup(s => s.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task HandleAsync_ShouldNormalizeEmail()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var query = new GetUserByEmailQuery("TEST@EXAMPLE.COM");

        _mockUserQueryStore
            .Setup(s => s.GetByEmailAsync(It.Is<Email>(e => e.Value == "test@example.com")))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task HandleAsync_ShouldCallQueryStore()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var query = new GetUserByEmailQuery("test@example.com");

        _mockUserQueryStore
            .Setup(s => s.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(query);

        // Assert
        _mockUserQueryStore.Verify(
            s => s.GetByEmailAsync(It.Is<Email>(e => e.Value == "test@example.com")),
            Times.Once
        );
    }

    // ──────────────────────────────────────────────────────────────
    // Error Cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithNonExistentEmail_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var query = new GetUserByEmailQuery(email);

        _mockUserQueryStore
            .Setup(s => s.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{email}*");
    }
}
