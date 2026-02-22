namespace PetAdoption.UserService.Tests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _handler = new RegisterUserCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldRegisterUser()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John Doe",
            "SecurePass123!",
            "+1234567890"
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(h => h.HashPassword(command.Password))
            .Returns("$2a$12$hashedpassword");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeEmpty();
        result.Message.Should().Be("User registered successfully");

        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
        _mockPasswordHasher.Verify(h => h.HashPassword(command.Password), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ShouldThrowDuplicateEmailException()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "existing@example.com",
            "John Doe",
            "SecurePass123!",
            "+1234567890"
        );

        var existingUser = User.Register(
            "existing@example.com",
            "Existing User",
            "$2a$12$hashedpassword",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<DuplicateEmailException>()
            .WithMessage("*existing@example.com*");

        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John Doe",
            "short", // Too short
            null
        );

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Password must be at least 8 characters*");

        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithoutPhoneNumber_ShouldSucceed()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John Doe",
            "SecurePass123!",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(h => h.HashPassword(command.Password))
            .Returns("$2a$12$hashedpassword");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldHashPassword()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John Doe",
            "PlainPassword123!",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(h => h.HashPassword("PlainPassword123!"))
            .Returns("$2a$12$hashedpassword");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockPasswordHasher.Verify(h => h.HashPassword("PlainPassword123!"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCheckEmailExistence()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John Doe",
            "SecurePass123!",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(h => h.HashPassword(command.Password))
            .Returns("$2a$12$hashedpassword");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(
            r => r.GetByEmailAsync(It.Is<Email>(e => e.Value == "test@example.com")),
            Times.Once
        );
    }
}
