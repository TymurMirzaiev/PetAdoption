namespace PetAdoption.UserService.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Status,
    string Role,
    UserPreferencesDto Preferences,
    DateTime RegisteredAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt
);

public record UserPreferencesDto(
    string? PreferredPetType,
    List<string>? PreferredSizes,
    string? PreferredAgeRange,
    bool ReceiveEmailNotifications,
    bool ReceiveSmsNotifications
);
