using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record ReorderPetPhotosCommand(
    Guid OrgId,
    Guid PetId,
    IReadOnlyList<Guid> OrderedIds,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<ReorderPetPhotosResponse>;

public record ReorderPetPhotosResponse(bool Success);

public class ReorderPetPhotosCommandHandler : IRequestHandler<ReorderPetPhotosCommand, ReorderPetPhotosResponse>
{
    private readonly IPetRepository _petRepository;

    public ReorderPetPhotosCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<ReorderPetPhotosResponse> Handle(
        ReorderPetPhotosCommand request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        pet.ReorderPhotos(request.OrderedIds);
        await _petRepository.Update(pet);

        return new ReorderPetPhotosResponse(true);
    }
}
