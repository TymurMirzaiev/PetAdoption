using PetAdoption.UserService.Application.Abstractions;

namespace PetAdoption.UserService.Application.Commands.Organizations;

public record AddOrganizationMemberCommand(Guid OrganizationId, string UserId, string Role) : ICommand<AddOrganizationMemberResponse>;
public record AddOrganizationMemberResponse(bool Success, string Message);
