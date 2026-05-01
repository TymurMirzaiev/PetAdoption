using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetMetricsQuery(
    Guid PetId,
    DateTime? From,
    DateTime? To) : IRequest<GetPetMetricsResponse>;

public record GetPetMetricsResponse(PetMetricsSummary Metrics);

public class GetPetMetricsQueryHandler : IRequestHandler<GetPetMetricsQuery, GetPetMetricsResponse>
{
    private readonly IPetMetricsQueryStore _queryStore;

    public GetPetMetricsQueryHandler(IPetMetricsQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<GetPetMetricsResponse> Handle(GetPetMetricsQuery request, CancellationToken ct = default)
    {
        var metrics = await _queryStore.GetMetricsByPetAsync(request.PetId, request.From, request.To)
            ?? throw new DomainException(PetDomainErrorCode.PetNotFound, $"Pet {request.PetId} not found.");
        return new GetPetMetricsResponse(metrics);
    }
}
