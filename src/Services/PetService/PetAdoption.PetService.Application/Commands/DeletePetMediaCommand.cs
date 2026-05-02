using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeletePetMediaCommand(
    Guid OrgId,
    Guid PetId,
    Guid MediaId,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<Unit>;

public class DeletePetMediaCommandHandler : IRequestHandler<DeletePetMediaCommand, Unit>
{
    private readonly IPetRepository _petRepository;
    private readonly IMediaStorage _mediaStorage;

    public DeletePetMediaCommandHandler(IPetRepository petRepository, IMediaStorage mediaStorage)
    {
        _petRepository = petRepository;
        _mediaStorage = mediaStorage;
    }

    public async Task<Unit> Handle(
        DeletePetMediaCommand request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetByIdOrThrowAsync(request.PetId);

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        var mediaItem = pet.Media.FirstOrDefault(m => m.Id == request.MediaId);
        if (mediaItem is null)
            throw new DomainException(
                PetDomainErrorCode.MediaNotFound,
                $"Media item {request.MediaId} was not found.",
                new Dictionary<string, object> { { "MediaId", request.MediaId } });

        var url = mediaItem.Url;

        // Delete the file first so that if storage deletion fails, the DB record is not orphaned.
        await _mediaStorage.DeleteAsync(url, ct);
        pet.RemoveMedia(request.MediaId);
        await _petRepository.Update(pet);

        return Unit.Value;
    }
}
