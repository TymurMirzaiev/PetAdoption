using PetAdoption.UserService.Domain.Entities;

namespace PetAdoption.UserService.Domain.Interfaces;

public interface IOrganizationMemberRepository
{
    Task AddAsync(OrganizationMember member);
    Task<OrganizationMember?> GetByOrgAndUserAsync(Guid organizationId, string userId);
    Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid organizationId);
    Task<IEnumerable<OrganizationMember>> GetByUserAsync(string userId);
    Task DeleteAsync(OrganizationMember member);
}
