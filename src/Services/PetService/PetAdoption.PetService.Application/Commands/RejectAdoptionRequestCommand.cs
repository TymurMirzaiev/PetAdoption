using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record RejectAdoptionRequestCommand(
    Guid RequestId,
    Guid ReviewerId,
    string Reason,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<RejectAdoptionRequestResponse>;
