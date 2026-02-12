using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public interface IPetRepository
{
    Task<IEnumerable<Pet>> GetAll();
    Task<Pet?> GetById(Guid id);
    Task Update(Pet pet);
}
