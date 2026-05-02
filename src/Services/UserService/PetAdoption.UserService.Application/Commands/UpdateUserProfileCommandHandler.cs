namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Helpers;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

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
        var user = await UserFetchHelper.GetUserOrThrowAsync(_userRepository, command.UserId);

        var preferences = command.Preferences is { } dto
            ? new UserPreferences
            {
                PreferredPetType = dto.PreferredPetType,
                PreferredSizes = dto.PreferredSizes,
                PreferredAgeRange = dto.PreferredAgeRange,
                ReceiveEmailNotifications = dto.ReceiveEmailNotifications,
                ReceiveSmsNotifications = dto.ReceiveSmsNotifications
            }
            : (UserPreferences?)null;

        user.UpdateProfile(
            command.FullName,
            command.PhoneNumber,
            preferences,
            command.Bio
        );

        await _userRepository.SaveAsync(user);

        return new UpdateUserProfileResponse(true, "Profile updated successfully");
    }
}
