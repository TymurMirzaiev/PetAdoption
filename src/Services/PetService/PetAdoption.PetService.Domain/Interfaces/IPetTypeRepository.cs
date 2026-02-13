namespace PetAdoption.PetService.Domain.Interfaces;

/// <summary>
/// Repository for managing pet type entities.
/// </summary>
public interface IPetTypeRepository
{
    /// <summary>
    /// Gets all active pet types.
    /// </summary>
    Task<IReadOnlyList<PetType>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pet types (including inactive).
    /// </summary>
    Task<IReadOnlyList<PetType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pet type by its unique identifier.
    /// </summary>
    Task<PetType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pet type by its code.
    /// </summary>
    Task<PetType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pet type with the given code already exists.
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new pet type.
    /// </summary>
    Task AddAsync(PetType petType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pet type.
    /// </summary>
    Task UpdateAsync(PetType petType, CancellationToken cancellationToken = default);
}
