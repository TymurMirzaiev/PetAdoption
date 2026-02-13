using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.DTOs;

/// <summary>
/// Standardized error response for API errors.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    /// Numeric error code value.
    /// </summary>
    public int ErrorCodeValue { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Additional error details or metadata.
    /// </summary>
    public IDictionary<string, object>? Details { get; init; }

    /// <summary>
    /// UTC timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    public ErrorResponse(
        PetDomainErrorCode errorCode,
        string message,
        IDictionary<string, object>? details = null)
    {
        ErrorCode = errorCode.ToString();
        ErrorCodeValue = (int)errorCode;
        Message = message;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }

    public ErrorResponse(
        string errorCode,
        int errorCodeValue,
        string message,
        IDictionary<string, object>? details = null)
    {
        ErrorCode = errorCode;
        ErrorCodeValue = errorCodeValue;
        Message = message;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
