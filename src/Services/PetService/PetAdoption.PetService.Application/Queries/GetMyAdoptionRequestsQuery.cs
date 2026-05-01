using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetMyAdoptionRequestsQuery(Guid UserId, int Skip = 0, int Take = 20)
    : IRequest<GetMyAdoptionRequestsResponse>;

public record AdoptionRequestDto(
    Guid Id,
    Guid PetId,
    string PetName,
    string PetType,
    Guid OrganizationId,
    string Status,
    string? Message,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record GetMyAdoptionRequestsResponse(
    List<AdoptionRequestDto> Items,
    long Total,
    int Skip,
    int Take);

public class GetMyAdoptionRequestsQueryHandler
    : IRequestHandler<GetMyAdoptionRequestsQuery, GetMyAdoptionRequestsResponse>
{
    private readonly IAdoptionRequestQueryStore _queryStore;
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetMyAdoptionRequestsQueryHandler(
        IAdoptionRequestQueryStore queryStore,
        IPetQueryStore petQueryStore,
        IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petQueryStore = petQueryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<GetMyAdoptionRequestsResponse> Handle(
        GetMyAdoptionRequestsQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _queryStore.GetByUserAsync(request.UserId, request.Skip, request.Take);

        var petTypes = await _petTypeRepository.GetAllAsync(cancellationToken);
        var petTypeDict = petTypes.ToDictionary(pt => pt.Id, pt => pt.Name);

        var dtos = new List<AdoptionRequestDto>();
        foreach (var item in items)
        {
            var pet = await _petQueryStore.GetById(item.PetId);
            var petTypeName = pet is not null && petTypeDict.TryGetValue(pet.PetTypeId, out var name) ? name : "Unknown";

            dtos.Add(new AdoptionRequestDto(
                item.Id,
                item.PetId,
                pet?.Name?.Value ?? "Unknown",
                petTypeName,
                item.OrganizationId,
                item.Status.ToString(),
                item.Message,
                item.RejectionReason,
                item.CreatedAt,
                item.ReviewedAt));
        }

        return new GetMyAdoptionRequestsResponse(dtos, total, request.Skip, request.Take);
    }
}
