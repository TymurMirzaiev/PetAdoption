namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public record ActivateUserResponse(bool Success, string Message);

public class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand, ActivateUserResponse>
{
    private readonly IUserRepository _userRepo;

    public ActivateUserCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ActivateUserResponse> HandleAsync(
        ActivateUserCommand command, CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new UserNotFoundException(command.UserId);

        user.Activate();
        await _userRepo.SaveAsync(user);

        return new ActivateUserResponse(true, "User activated successfully");
    }
}
