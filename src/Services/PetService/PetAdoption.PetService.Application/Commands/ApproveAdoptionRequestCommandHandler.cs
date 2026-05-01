using PetAdoption.PetService.Application.Abstractions;
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

        adoptionRequest.Approve();

        var pet = await _petRepository.GetById(adoptionRequest.PetId)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {adoptionRequest.PetId} not found.",
                new Dictionary<string, object> { { "PetId", adoptionRequest.PetId } });

        pet.Reserve();

        await _petRepository.Update(pet);
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new ApproveAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
