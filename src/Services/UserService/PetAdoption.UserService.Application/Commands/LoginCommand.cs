namespace PetAdoption.UserService.Application.Commands;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResponse>;
