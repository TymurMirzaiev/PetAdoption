namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record GoogleAuthCommand(string IdToken) : ICommand<GoogleAuthResponse>;

public record GoogleAuthResponse(string AccessToken, string RefreshToken);
