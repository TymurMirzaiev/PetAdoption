using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UploadPetMediaCommand(
    Guid OrgId,
    Guid PetId,
    Stream Content,
    string ContentType,
    string FileName,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<UploadPetMediaResponse>;

public record UploadPetMediaResponse(Guid Id, string Url, string MediaType);

public class UploadPetMediaCommandHandler : IRequestHandler<UploadPetMediaCommand, UploadPetMediaResponse>
{
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "video/mp4"
        };

    private readonly IPetRepository _petRepository;
    private readonly IMediaStorage _mediaStorage;

    public UploadPetMediaCommandHandler(IPetRepository petRepository, IMediaStorage mediaStorage)
    {
        _petRepository = petRepository;
        _mediaStorage = mediaStorage;
    }

    public async Task<UploadPetMediaResponse> Handle(
        UploadPetMediaCommand request, CancellationToken ct = default)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                $"Content type '{request.ContentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}.",
                new Dictionary<string, object> { { "ContentType", request.ContentType } });

        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        var uploadResult = await _mediaStorage.UploadAsync(
            request.Content, request.ContentType, request.FileName, ct);

        var mediaId = Guid.NewGuid();
        var isVideo = request.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        if (isVideo)
            pet.AddVideo(mediaId, uploadResult.Url, request.ContentType);
        else
            pet.AddPhoto(mediaId, uploadResult.Url, request.ContentType);

        await _petRepository.Update(pet);

        return new UploadPetMediaResponse(
            mediaId,
            uploadResult.Url,
            isVideo ? "Video" : "Photo");
    }
}
