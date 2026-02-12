using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

/// <summary>
/// Query-side repository for read-optimized pet data access.
/// Separate from IPetRepository to maintain CQRS separation.
/// </summary>
public interface IPetQueryStore
{
    Task<IEnumerable<Pet>> GetAll();
    Task<Pet?> GetById(Guid id);
}
