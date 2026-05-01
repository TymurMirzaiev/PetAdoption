using PetAdoption.UserService.Application.Abstractions;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record UpdateOrganizationCommand(Guid Id, string Name, string? Description) : ICommand<UpdateOrganizationResponse>;
public record UpdateOrganizationResponse(bool Success, string Message);
