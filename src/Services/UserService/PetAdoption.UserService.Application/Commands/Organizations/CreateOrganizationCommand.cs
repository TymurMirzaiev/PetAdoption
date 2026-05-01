using PetAdoption.UserService.Application.Abstractions;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record CreateOrganizationCommand(string Name, string Slug, string? Description) : ICommand<CreateOrganizationResponse>;
public record CreateOrganizationResponse(bool Success, Guid OrganizationId, string Message);
