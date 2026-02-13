namespace PetAdoption.PetService.Domain.Exceptions;

/// <summary>
/// Domain exception with error code for programmatic handling.
/// All domain errors are represented by this single exception type with different error codes.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Error code for programmatic error handling by API clients.
    /// </summary>
    public PetDomainErrorCode ErrorCode { get; }

    /// <summary>
    /// Additional metadata about the error (e.g., PetId, CurrentStatus, AttemptedValue).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    public DomainException(PetDomainErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(
        PetDomainErrorCode errorCode,
        string message,
        IDictionary<string, object> metadata)
        : base(message)
    {
        ErrorCode = errorCode;
        Metadata = metadata as IReadOnlyDictionary<string, object>
                   ?? new Dictionary<string, object>(metadata);
    }

    public DomainException(
        PetDomainErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
