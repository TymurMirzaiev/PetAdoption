using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CancelAdoptionRequestCommand(Guid RequestId, Guid UserId) : IRequest<CancelAdoptionRequestResponse>;
