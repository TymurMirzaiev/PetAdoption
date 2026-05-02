namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

public record TrackSkipResponse(Guid Id, Guid PetId, DateTime CreatedAt);

public class TrackSkipCommandHandler : IRequestHandler<TrackSkipCommand, TrackSkipResponse>
{
    private readonly IPetSkipRepository _skipRepository;
    private readonly IPetRepository _petRepository;

    public TrackSkipCommandHandler(IPetSkipRepository skipRepository, IPetRepository petRepository)
    {
        _skipRepository = skipRepository;
        _petRepository = petRepository;
    }

    public async Task<TrackSkipResponse> Handle(TrackSkipCommand request, CancellationToken ct)
    {
        _ = await _petRepository.GetByIdOrThrowAsync(request.PetId);

        var existing = await _skipRepository.GetByUserAndPetAsync(request.UserId, request.PetId);
        if (existing is not null)
            throw new DomainException(PetDomainErrorCode.SkipAlreadyExists, "Pet is already skipped.");

        var skip = PetSkip.Create(request.UserId, request.PetId);
        await _skipRepository.AddAsync(skip);

        return new TrackSkipResponse(skip.Id, skip.PetId, skip.CreatedAt);
    }
}
