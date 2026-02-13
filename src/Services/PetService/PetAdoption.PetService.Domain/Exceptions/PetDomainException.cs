namespace PetAdoption.PetService.Domain.Exceptions;

/// <summary>
/// Domain exception with string-based error code for programmatic handling.
/// All domain errors are represented by this single exception type with different error codes.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Error code (subcode) for programmatic error handling by API clients.
    /// Uses snake_case format (e.g., "pet_not_found", "invalid_pet_name").
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Additional metadata about the error (e.g., PetId, CurrentStatus, AttemptedValue).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    public DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
    }

    public DomainException(
        string errorCode,
        string message,
        IDictionary<string, object> metadata)
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        Metadata = metadata as IReadOnlyDictionary<string, object>
                   ?? new Dictionary<string, object>(metadata);
    }

    public DomainException(
        string errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
    }
}
