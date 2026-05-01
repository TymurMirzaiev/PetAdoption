using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record ApproveAdoptionRequestCommand(
    Guid RequestId,
    Guid ReviewerId,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<ApproveAdoptionRequestResponse>;
