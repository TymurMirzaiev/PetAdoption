using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Interfaces;

namespace PetAdoption.UserService.Application.Queries.Organizations;

public record GetOrganizationByIdQuery(Guid Id) : IQuery<OrganizationDetailResponse?>;
public record OrganizationDetailResponse(Guid Id, string Name, string Slug, string? Description, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public class GetOrganizationByIdQueryHandler : IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse?>
{
    private readonly IOrganizationRepository _orgRepo;
    public GetOrganizationByIdQueryHandler(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    public async Task<OrganizationDetailResponse?> HandleAsync(GetOrganizationByIdQuery query, CancellationToken cancellationToken = default)
    {
        var org = await _orgRepo.GetByIdAsync(query.Id);
        if (org is null) return null;
        return new OrganizationDetailResponse(org.Id, org.Name, org.Slug, org.Description, org.Status == OrganizationStatus.Active, org.CreatedAt, org.UpdatedAt);
    }
}
