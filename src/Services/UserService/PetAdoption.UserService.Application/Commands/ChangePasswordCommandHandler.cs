namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Helpers;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResponse> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate new password
        Password.ValidatePlainText(command.NewPassword);

        var user = await UserFetchHelper.GetUserOrThrowAsync(_userRepository, command.UserId);

        // Verify current password
        if (user.Password is null)
            throw new InvalidCredentialsException();

        var isCurrentPasswordValid = _passwordHasher.VerifyPassword(
            command.CurrentPassword,
            user.Password.HashedValue
        );

        if (!isCurrentPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        // Hash and set new password
        var newHashedPassword = _passwordHasher.HashPassword(command.NewPassword);
        user.ChangePassword(newHashedPassword);

        await _userRepository.SaveAsync(user);

        return new ChangePasswordResponse(true, "Password changed successfully");
    }
}
