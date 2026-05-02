namespace PetAdoption.UserService.Application.Commands;

using Microsoft.Extensions.Options;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Options;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class GoogleAuthCommandHandler : ICommandHandler<GoogleAuthCommand, GoogleAuthResponse>
{
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly JwtApplicationOptions _jwtOptions;

    public GoogleAuthCommandHandler(
        IGoogleTokenValidator googleValidator,
        IUserRepository userRepo,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepo,
        IOptions<JwtApplicationOptions> jwtOptions)
    {
        _googleValidator = googleValidator;
        _userRepo = userRepo;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<GoogleAuthResponse> HandleAsync(
        GoogleAuthCommand command, CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleValidator.ValidateAsync(command.IdToken)
            ?? throw new InvalidCredentialsException();

        var email = Email.From(googleUser.Email);
        var user = await _userRepo.GetByEmailAsync(email);

        if (user is null)
        {
            user = User.RegisterFromGoogle(googleUser.Email, googleUser.FullName);
            await _userRepo.SaveAsync(user);
        }
        else
        {
            if (user.Status == UserStatus.Suspended)
                throw new UserSuspendedException(user.Id.Value);

            user.RecordLogin();
            await _userRepo.SaveAsync(user);
        }

        var accessToken = _jwtGenerator.GenerateToken(
            user.Id.Value, user.Email.Value, user.Role.ToString());

        var refreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(_jwtOptions.RefreshTokenLifetimeDays));
        await _refreshTokenRepo.SaveAsync(refreshToken);

        return new GoogleAuthResponse(accessToken, refreshToken.Token);
    }
}
