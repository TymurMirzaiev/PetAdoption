using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record RejectAdoptionRequestResponse(Guid Id, string Status);

public class RejectAdoptionRequestCommandHandler
    : IRequestHandler<RejectAdoptionRequestCommand, RejectAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;

    public RejectAdoptionRequestCommandHandler(IAdoptionRequestRepository adoptionRequestRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
    }

    public async Task<RejectAdoptionRequestResponse> Handle(
        RejectAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        adoptionRequest.Reject(request.Reason);
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new RejectAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
