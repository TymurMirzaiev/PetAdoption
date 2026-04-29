namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken);
