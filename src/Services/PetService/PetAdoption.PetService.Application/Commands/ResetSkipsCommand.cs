namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record ResetSkipsCommand(Guid UserId) : IRequest<ResetSkipsResponse>;
