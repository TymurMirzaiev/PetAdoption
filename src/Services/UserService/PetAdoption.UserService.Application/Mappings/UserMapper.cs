namespace PetAdoption.UserService.Application.Mappings;

using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Entities;

public static class UserMapper
{
    public static UserDto ToDto(User user) => new(
        user.Id.Value,
        user.Email.Value,
        user.FullName.Value,
        user.PhoneNumber?.Value,
        user.Status.ToString(),
        user.Role.ToString(),
        new UserPreferencesDto(
            user.Preferences.PreferredPetType,
            user.Preferences.PreferredSizes,
            user.Preferences.PreferredAgeRange,
            user.Preferences.ReceiveEmailNotifications,
            user.Preferences.ReceiveSmsNotifications
        ),
        user.ExternalProvider,
        user.HasPassword,
        user.RegisteredAt,
        user.UpdatedAt,
        user.LastLoginAt,
        user.Bio?.Value
    );
}
