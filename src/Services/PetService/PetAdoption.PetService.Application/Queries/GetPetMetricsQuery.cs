using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetMetricsQuery(
    Guid PetId,
    DateTime? From,
    DateTime? To,
    Guid? CallerOrgId,
    string? CallerOrgRole) : IRequest<GetPetMetricsResponse>;

public record GetPetMetricsResponse(PetMetricsSummary Metrics);

public class GetPetMetricsQueryHandler : IRequestHandler<GetPetMetricsQuery, GetPetMetricsResponse>
{
    private readonly IPetMetricsQueryStore _queryStore;
    private readonly IPetRepository _petRepository;

    public GetPetMetricsQueryHandler(IPetMetricsQueryStore queryStore, IPetRepository petRepository)
    {
        _queryStore = queryStore;
        _petRepository = petRepository;
    }

    public async Task<GetPetMetricsResponse> Handle(GetPetMetricsQuery request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetById(request.PetId)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");

        if (pet.OrganizationId is null)
            throw new DomainException(
                PetDomainErrorCode.NotAuthorizedForOrg,
                "Pet is not associated with any organization.",
                new Dictionary<string, object> { { "PetId", pet.Id } });

        OrgAuthorization.EnsureMember(pet.OrganizationId.Value, request.CallerOrgId, request.CallerOrgRole);

        var metrics = await _queryStore.GetMetricsByPetAsync(request.PetId, request.From, request.To)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");
        return new GetPetMetricsResponse(metrics);
    }
}
