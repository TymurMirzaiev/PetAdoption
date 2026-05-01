using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record TrackBatchImpressionsCommand(
    IEnumerable<Guid> PetIds,
    Guid UserId) : IRequest<TrackBatchImpressionsResponse>;
