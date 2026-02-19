namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<ChangePasswordResponse>;

public record ChangePasswordResponse(bool Success, string Message);
