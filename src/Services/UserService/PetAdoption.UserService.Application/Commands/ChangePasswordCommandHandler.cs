namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

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

        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        // Verify current password
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
