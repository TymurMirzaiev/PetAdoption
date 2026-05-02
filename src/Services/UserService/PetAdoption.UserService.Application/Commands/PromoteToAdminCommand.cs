namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Helpers;
using PetAdoption.UserService.Domain.Interfaces;

public record PromoteToAdminCommand(string UserId) : ICommand<PromoteToAdminResponse>;

public record PromoteToAdminResponse(bool Success, string Message);

public class PromoteToAdminCommandHandler : ICommandHandler<PromoteToAdminCommand, PromoteToAdminResponse>
{
    private readonly IUserRepository _userRepository;

    public PromoteToAdminCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PromoteToAdminResponse> HandleAsync(
        PromoteToAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await UserFetchHelper.GetUserOrThrowAsync(_userRepository, command.UserId);

        user.PromoteToAdmin();
        await _userRepository.SaveAsync(user);

        return new PromoteToAdminResponse(true, "User promoted to admin successfully");
    }
}
