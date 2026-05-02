namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record UpdateUserProfileCommand(
    string UserId,
    string? FullName = null,
    string? PhoneNumber = null,
    UpdatePreferencesDto? Preferences = null,
    string? Bio = null
) : ICommand<UpdateUserProfileResponse>;

public record UpdateUserProfileResponse(bool Success, string Message);
