namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResponse> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate password requirements
        Password.ValidatePlainText(command.Password);

        // Check if email already exists
        var email = Email.From(command.Email);
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
        {
            throw new DuplicateEmailException(command.Email);
        }

        // Hash password
        var hashedPassword = _passwordHasher.HashPassword(command.Password);

        // Create new user
        var user = User.Register(
            command.Email,
            command.FullName,
            hashedPassword,
            command.PhoneNumber
        );

        // Save user (will publish events via outbox)
        await _userRepository.SaveAsync(user);

        return new RegisterUserResponse(
            Success: true,
            UserId: user.Id.Value,
            Message: "User registered successfully"
        );
    }
}
