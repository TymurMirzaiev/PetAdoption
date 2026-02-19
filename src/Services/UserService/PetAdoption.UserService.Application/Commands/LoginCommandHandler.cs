namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Enums;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        // Find user by email
        var email = Email.From(command.Email);
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            // Don't reveal whether user exists
            throw new InvalidCredentialsException();
        }

        // Check if user is suspended
        if (user.Status == UserStatus.Suspended)
        {
            throw new UserSuspendedException(user.Id.Value);
        }

        // Verify password
        var isPasswordValid = _passwordHasher.VerifyPassword(
            command.Password,
            user.Password.HashedValue
        );

        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(
            user.Id.Value,
            user.Email.Value,
            user.Role.ToString()
        );

        // Record login
        user.RecordLogin();
        await _userRepository.SaveAsync(user);

        return new LoginResponse(
            Success: true,
            Token: token,
            UserId: user.Id.Value,
            Email: user.Email.Value,
            FullName: user.FullName.Value,
            Role: user.Role.ToString(),
            ExpiresIn: 3600 // 1 hour in seconds
        );
    }
}
