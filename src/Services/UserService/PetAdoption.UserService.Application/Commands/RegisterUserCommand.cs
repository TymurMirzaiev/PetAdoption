namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record RegisterUserCommand(
    string Email,
    string FullName,
    string Password,
    string? PhoneNumber = null
) : ICommand<RegisterUserResponse>;
