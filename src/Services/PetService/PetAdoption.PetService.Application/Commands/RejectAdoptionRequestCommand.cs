using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record RejectAdoptionRequestCommand(Guid RequestId, Guid ReviewerId, string Reason) : IRequest<RejectAdoptionRequestResponse>;
