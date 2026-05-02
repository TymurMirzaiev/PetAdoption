using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record TrackInteractionResponse(Guid Id);

public class TrackInteractionCommandHandler : IRequestHandler<TrackInteractionCommand, TrackInteractionResponse>
{
    private readonly IPetInteractionRepository _repository;
    private readonly IPetRepository _petRepository;

    public TrackInteractionCommandHandler(IPetInteractionRepository repository, IPetRepository petRepository)
    {
        _repository = repository;
        _petRepository = petRepository;
    }

    public async Task<TrackInteractionResponse> Handle(TrackInteractionCommand request, CancellationToken ct = default)
    {
        _ = await _petRepository.GetByIdOrThrowAsync(request.PetId);

        var interaction = PetInteraction.Create(request.PetId, request.UserId, request.Type);
        await _repository.AddAsync(interaction);
        return new TrackInteractionResponse(interaction.Id);
    }
}
