namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

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
        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        user.PromoteToAdmin();
        await _userRepository.SaveAsync(user);

        return new PromoteToAdminResponse(true, "User promoted to admin successfully");
    }
}
