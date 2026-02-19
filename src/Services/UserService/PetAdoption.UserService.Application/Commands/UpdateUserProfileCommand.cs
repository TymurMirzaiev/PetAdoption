namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.ValueObjects;

public record UpdateUserProfileCommand(
    string UserId,
    string? FullName = null,
    string? PhoneNumber = null,
    UserPreferences? Preferences = null
) : ICommand<UpdateUserProfileResponse>;

public record UpdateUserProfileResponse(bool Success, string Message);
