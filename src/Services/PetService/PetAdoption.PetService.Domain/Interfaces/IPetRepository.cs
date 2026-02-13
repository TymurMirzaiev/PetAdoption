namespace PetAdoption.PetService.Domain.Interfaces;

/// <summary>
/// Repository for Pet aggregate root - command-side operations only.
/// For queries, use IPetQueryStore in the Application layer.
/// </summary>
public interface IPetRepository
{
    Task<Pet?> GetById(Guid id);
    Task Update(Pet pet);
    Task Add(Pet pet);
}
