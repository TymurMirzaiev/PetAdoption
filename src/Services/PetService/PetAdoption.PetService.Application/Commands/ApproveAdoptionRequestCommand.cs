using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record ApproveAdoptionRequestCommand(Guid RequestId, Guid ReviewerId) : IRequest<ApproveAdoptionRequestResponse>;
