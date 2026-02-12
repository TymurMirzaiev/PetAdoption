namespace PetAdoption.PetService.Domain.Exceptions;

/// <summary>
/// Error codes for pet domain operations.
/// These codes are exposed to API clients for programmatic error handling.
/// </summary>
public enum PetDomainErrorCode
{
    // Pet aggregate errors (1000-1999)

    /// <summary>
    /// Pet cannot be reserved because it is not available.
    /// </summary>
    PetNotAvailable = 1001,

    /// <summary>
    /// Pet cannot be adopted or have reservation cancelled because it is not reserved.
    /// </summary>
    PetNotReserved = 1002,

    /// <summary>
    /// Pet was not found in the system.
    /// </summary>
    PetNotFound = 1003,

    // Value object validation errors (2000-2999)

    /// <summary>
    /// Pet name is invalid (empty, too long, etc.).
    /// </summary>
    InvalidPetName = 2001,

    /// <summary>
    /// Pet type is invalid (not in allowed list).
    /// </summary>
    InvalidPetType = 2002,

    // General domain errors (9000-9999)

    /// <summary>
    /// Invalid operation or business rule violation.
    /// </summary>
    InvalidOperation = 9001,

    /// <summary>
    /// Unknown domain error.
    /// </summary>
    UnknownDomainError = 9999
}
