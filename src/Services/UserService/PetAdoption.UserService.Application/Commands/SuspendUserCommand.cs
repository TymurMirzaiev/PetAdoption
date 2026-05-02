namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Helpers;
using PetAdoption.UserService.Domain.Interfaces;

public record SuspendUserCommand(string UserId, string Reason) : ICommand<SuspendUserResponse>;

public record SuspendUserResponse(bool Success, string Message);

public class SuspendUserCommandHandler : ICommandHandler<SuspendUserCommand, SuspendUserResponse>
{
    private readonly IUserRepository _userRepository;

    public SuspendUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<SuspendUserResponse> HandleAsync(
        SuspendUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await UserFetchHelper.GetUserOrThrowAsync(_userRepository, command.UserId);

        user.Suspend(command.Reason);
        await _userRepository.SaveAsync(user);

        return new SuspendUserResponse(true, $"User suspended: {command.Reason}");
    }
}
