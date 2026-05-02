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
    string? ReviewerOrgRole) : IRequest<DeletePetMediaResponse>;

public record DeletePetMediaResponse(bool Success);

public class DeletePetMediaCommandHandler : IRequestHandler<DeletePetMediaCommand, DeletePetMediaResponse>
{
    private readonly IPetRepository _petRepository;
    private readonly IMediaStorage _mediaStorage;

    public DeletePetMediaCommandHandler(IPetRepository petRepository, IMediaStorage mediaStorage)
    {
        _petRepository = petRepository;
        _mediaStorage = mediaStorage;
    }

    public async Task<DeletePetMediaResponse> Handle(
        DeletePetMediaCommand request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        var mediaItem = pet.Media.FirstOrDefault(m => m.Id == request.MediaId);
        if (mediaItem is null)
            throw new DomainException(
                PetDomainErrorCode.MediaNotFound,
                $"Media item {request.MediaId} was not found.",
                new Dictionary<string, object> { { "MediaId", request.MediaId } });

        var url = mediaItem.Url;

        pet.RemoveMedia(request.MediaId);
        await _petRepository.Update(pet);
        await _mediaStorage.DeleteAsync(url, ct);

        return new DeletePetMediaResponse(true);
    }
}
