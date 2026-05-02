using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record SetPrimaryPhotoCommand(
    Guid OrgId,
    Guid PetId,
    Guid MediaId,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<Unit>;

public class SetPrimaryPhotoCommandHandler : IRequestHandler<SetPrimaryPhotoCommand, Unit>
{
    private readonly IPetRepository _petRepository;

    public SetPrimaryPhotoCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<Unit> Handle(
        SetPrimaryPhotoCommand request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetByIdOrThrowAsync(request.PetId);

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        pet.SetPrimaryPhoto(request.MediaId);
        await _petRepository.Update(pet);

        return Unit.Value;
    }
}
