using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record TrackInteractionResponse(Guid Id);

public class TrackInteractionCommandHandler : IRequestHandler<TrackInteractionCommand, TrackInteractionResponse>
{
    private readonly IPetInteractionRepository _repository;

    public TrackInteractionCommandHandler(IPetInteractionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrackInteractionResponse> Handle(TrackInteractionCommand request, CancellationToken ct = default)
    {
        var interaction = PetInteraction.Create(request.PetId, request.UserId, request.Type);
        await _repository.AddAsync(interaction);
        return new TrackInteractionResponse(interaction.Id);
    }
}
