using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record TrackBatchImpressionsResponse(int Count);

public class TrackBatchImpressionsCommandHandler
    : IRequestHandler<TrackBatchImpressionsCommand, TrackBatchImpressionsResponse>
{
    private readonly IPetInteractionRepository _repository;

    public TrackBatchImpressionsCommandHandler(IPetInteractionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrackBatchImpressionsResponse> Handle(
        TrackBatchImpressionsCommand request, CancellationToken ct = default)
    {
        var interactions = request.PetIds
            .Select(petId => PetInteraction.Create(petId, request.UserId, InteractionType.Impression))
            .ToList();

        await _repository.AddBatchAsync(interactions);
        return new TrackBatchImpressionsResponse(interactions.Count);
    }
}
