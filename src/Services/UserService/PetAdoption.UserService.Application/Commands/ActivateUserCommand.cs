namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record ActivateUserCommand(string UserId) : ICommand<ActivateUserResponse>;
