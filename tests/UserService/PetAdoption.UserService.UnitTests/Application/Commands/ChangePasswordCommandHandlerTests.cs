namespace PetAdoption.UserService.Tests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _handler = new ChangePasswordCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object
        );
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldChangePassword()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$oldhashedpassword",
            null
        );

        var command = new ChangePasswordCommand(
            user.Id.Value,
            "OldPassword123!",
            "NewPassword123!"
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("OldPassword123!", "$2a$12$oldhashedpassword"))
            .Returns(true);

        _mockPasswordHasher
            .Setup(h => h.HashPassword("NewPassword123!"))
            .Returns("$2a$12$newhashedpassword");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Password changed successfully");

        _mockUserRepository.Verify(r => r.SaveAsync(user), Times.Once);
        _mockPasswordHasher.Verify(h => h.HashPassword("NewPassword123!"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new ChangePasswordCommand(
            userId,
            "OldPassword123!",
            "NewPassword123!"
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{userId}*");

        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithIncorrectCurrentPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var command = new ChangePasswordCommand(
            user.Id.Value,
            "WrongPassword123!",
            "NewPassword123!"
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("WrongPassword123!", "$2a$12$hashedpassword"))
            .Returns(false);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidNewPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var command = new ChangePasswordCommand(
            user.Id.Value,
            "OldPassword123!",
            "short" // Too short
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Password must be at least 8 characters*");

        _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithSuspendedUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );
        user.Suspend("Account suspended");

        var command = new ChangePasswordCommand(
            user.Id.Value,
            "OldPassword123!",
            "NewPassword123!"
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("OldPassword123!", "$2a$12$hashedpassword"))
            .Returns(true);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*suspended*");
    }

    [Fact]
    public async Task HandleAsync_ShouldVerifyCurrentPasswordBeforeChanging()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var command = new ChangePasswordCommand(
            user.Id.Value,
            "CurrentPassword123!",
            "NewPassword123!"
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("CurrentPassword123!", "$2a$12$hashedpassword"))
            .Returns(true);

        _mockPasswordHasher
            .Setup(h => h.HashPassword("NewPassword123!"))
            .Returns("$2a$12$newhashedpassword");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockPasswordHasher.Verify(
            h => h.VerifyPassword("CurrentPassword123!", "$2a$12$hashedpassword"),
            Times.Once
        );
    }
}
