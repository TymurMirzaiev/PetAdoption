namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UpdateUserProfileResponse> HandleAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        user.UpdateProfile(
            command.FullName,
            command.PhoneNumber,
            command.Preferences
        );

        await _userRepository.SaveAsync(user);

        return new UpdateUserProfileResponse(true, "Profile updated successfully");
    }
}
