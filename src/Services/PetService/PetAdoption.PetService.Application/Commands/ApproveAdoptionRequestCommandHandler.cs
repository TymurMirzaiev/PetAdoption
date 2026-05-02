using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record ApproveAdoptionRequestResponse(Guid Id, string Status);

public class ApproveAdoptionRequestCommandHandler
    : IRequestHandler<ApproveAdoptionRequestCommand, ApproveAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IPetRepository _petRepository;

    public ApproveAdoptionRequestCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IPetRepository petRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _petRepository = petRepository;
    }

    public async Task<ApproveAdoptionRequestResponse> Handle(
        ApproveAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        OrgAuthorization.EnsureMember(
            adoptionRequest.OrganizationId, request.ReviewerOrgId, request.ReviewerOrgRole);

        var pet = await _petRepository.GetByIdOrThrowAsync(adoptionRequest.PetId);

        if (pet.Status != PetStatus.Available)
            throw new DomainException(PetDomainErrorCode.PetNotAvailable, "Pet is no longer available.");

        adoptionRequest.Approve();
        pet.Reserve();

        // UpdateAsync adds outbox events for adoptionRequest domain events and calls SaveChangesAsync.
        // EF Core change tracking ensures pet.Reserve() state is persisted in the same save.
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new ApproveAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
