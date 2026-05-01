using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CancelAdoptionRequestResponse(Guid Id, string Status);

public class CancelAdoptionRequestCommandHandler
    : IRequestHandler<CancelAdoptionRequestCommand, CancelAdoptionRequestResponse>
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;

    public CancelAdoptionRequestCommandHandler(IAdoptionRequestRepository adoptionRequestRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
    }

    public async Task<CancelAdoptionRequestResponse> Handle(
        CancelAdoptionRequestCommand request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new DomainException(
                PetDomainErrorCode.AdoptionRequestNotFound,
                $"Adoption request {request.RequestId} not found.",
                new Dictionary<string, object> { { "RequestId", request.RequestId } });

        if (adoptionRequest.UserId != request.UserId)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                "Only the requesting user can cancel their adoption request.",
                new Dictionary<string, object>
                {
                    { "RequestId", request.RequestId },
                    { "RequestUserId", adoptionRequest.UserId },
                    { "CallerUserId", request.UserId }
                });
        }

        adoptionRequest.Cancel();
        await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

        return new CancelAdoptionRequestResponse(adoptionRequest.Id, adoptionRequest.Status.ToString());
    }
}
