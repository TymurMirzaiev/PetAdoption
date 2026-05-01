using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAdoptionRequestResponse(Guid Id, string Status);

public class CreateAdoptionRequestCommandHandler
    : IRequestHandler<CreateAdoptionRequestCommand, CreateAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IPetQueryStore _petQueryStore;

    public CreateAdoptionRequestCommandHandler(
        IAdoptionRequestRepository adoptionRequestRepository,
        IPetQueryStore petQueryStore)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _petQueryStore = petQueryStore;
    }

    public async Task<CreateAdoptionRequestResponse> Handle(
        CreateAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _petQueryStore.GetById(request.PetId);
        if (pet is null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {request.PetId} not found.",
                new Dictionary<string, object> { { "PetId", request.PetId } });
        }

        if (pet.Status != PetStatus.Available)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotAvailable,
                $"Pet {request.PetId} is not available for adoption.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId },
                    { "Status", pet.Status.ToString() }
                });
        }

        var existing = await _adoptionRequestRepository.GetPendingByUserAndPetAsync(
            request.UserId, request.PetId, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException(
                PetDomainErrorCode.AdoptionRequestAlreadyExists,
                "A pending adoption request already exists for this pet.",
                new Dictionary<string, object>
                {
                    { "UserId", request.UserId },
                    { "PetId", request.PetId },
                    { "ExistingRequestId", existing.Id }
                });
        }

        var organizationId = pet.OrganizationId ?? throw new DomainException(
            PetDomainErrorCode.InvalidOperation,
            "Pet is not assigned to an organization.",
            new Dictionary<string, object> { { "PetId", request.PetId } });

        var adoptionRequest = AdoptionRequest.Create(
            request.UserId, request.PetId, organizationId, request.Message);

        await _adoptionRequestRepository.AddAsync(adoptionRequest, cancellationToken);

        return new CreateAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
