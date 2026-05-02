namespace PetAdoption.UserService.Application.Commands;

using Microsoft.Extensions.Options;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Options;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly JwtApplicationOptions _jwtOptions;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        IJwtTokenGenerator jwtGenerator,
        IOptions<JwtApplicationOptions> jwtOptions)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _jwtGenerator = jwtGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RefreshTokenResponse> HandleAsync(
        RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _refreshTokenRepo.GetByTokenAsync(command.RefreshToken);
        if (existing is null || !existing.IsValid)
            throw new InvalidCredentialsException();

        existing.Revoke();
        await _refreshTokenRepo.SaveAsync(existing);

        var user = await _userRepo.GetByIdAsync(UserId.From(existing.UserId))
            ?? throw new UserNotFoundException(existing.UserId);

        var accessToken = _jwtGenerator.GenerateToken(
            user.Id.Value, user.Email.Value, user.Role.ToString());

        var newRefreshToken = RefreshToken.Create(user.Id.Value, TimeSpan.FromDays(_jwtOptions.RefreshTokenLifetimeDays));
        await _refreshTokenRepo.SaveAsync(newRefreshToken);

        return new RefreshTokenResponse(accessToken, newRefreshToken.Token);
    }
}
