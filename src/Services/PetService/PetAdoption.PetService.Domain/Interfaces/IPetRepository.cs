using PetAdoption.PetService.Domain.Exceptions;

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
    Task Delete(Guid id);

    async Task<Pet> GetByIdOrThrowAsync(Guid id)
    {
        return await GetById(id)
            ?? throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet {id} not found.",
                new Dictionary<string, object> { { "PetId", id } });
    }
}
