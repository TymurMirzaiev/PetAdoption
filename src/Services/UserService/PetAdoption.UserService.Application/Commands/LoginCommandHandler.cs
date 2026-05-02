namespace PetAdoption.UserService.Application.Commands;

using Microsoft.Extensions.Options;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Application.Options;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly JwtApplicationOptions _jwtOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepo,
        IOptions<JwtApplicationOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtOptions = jwtOptions.Value;
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
        if (user.Password is null)
            throw new InvalidCredentialsException();

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
            user.Role.ToString(),
            bio: user.Bio?.Value
        );

        // Create refresh token
        var refreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(_jwtOptions.RefreshTokenLifetimeDays));
        await _refreshTokenRepo.SaveAsync(refreshToken);

        // Record login
        user.RecordLogin();
        await _userRepository.SaveAsync(user);

        return new LoginResponse(
            Success: true,
            Token: token,
            RefreshToken: refreshToken.Token,
            UserId: user.Id.Value,
            Email: user.Email.Value,
            FullName: user.FullName.Value,
            Role: user.Role.ToString(),
            ExpiresIn: (int)TimeSpan.FromMinutes(_jwtOptions.ExpirationMinutes).TotalSeconds
        );
    }
}
