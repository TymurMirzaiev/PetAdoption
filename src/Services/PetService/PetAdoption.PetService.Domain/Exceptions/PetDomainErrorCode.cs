namespace PetAdoption.PetService.Domain.Exceptions;

/// <summary>
/// Error codes for pet domain operations using snake_case subcodes.
/// These codes are exposed to API clients for programmatic error handling.
/// </summary>
public static class PetDomainErrorCode
{
    // Pet aggregate errors

    /// <summary>
    /// Pet cannot be reserved because it is not available.
    /// </summary>
    public const string PetNotAvailable = "pet_not_available";

    /// <summary>
    /// Pet cannot be adopted or have reservation cancelled because it is not reserved.
    /// </summary>
    public const string PetNotReserved = "pet_not_reserved";

    /// <summary>
    /// Pet was not found in the system.
    /// </summary>
    public const string PetNotFound = "pet_not_found";

    /// <summary>
    /// Pet was modified by another operation (optimistic concurrency conflict).
    /// </summary>
    public const string ConcurrencyConflict = "concurrency_conflict";

    // Value object validation errors

    /// <summary>
    /// Pet name is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidPetName = "invalid_pet_name";

    /// <summary>
    /// Pet type is invalid (not in allowed list).
    /// </summary>
    public const string InvalidPetType = "invalid_pet_type";

    // General domain errors

    /// <summary>
    /// Invalid operation or business rule violation.
    /// </summary>
    public const string InvalidOperation = "invalid_operation";

    /// <summary>
    /// Unknown domain error.
    /// </summary>
    public const string UnknownDomainError = "unknown_domain_error";
}
