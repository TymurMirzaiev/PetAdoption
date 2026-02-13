namespace PetAdoption.PetService.Application.DTOs;

/// <summary>
/// Standardized error response for API errors with string-based error codes.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error code (subcode) for programmatic handling.
    /// Uses snake_case format (e.g., "pet_not_found", "invalid_pet_name").
    /// </summary>
    public string ErrorCode { get; init; }

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
        string errorCode,
        string message,
        IDictionary<string, object>? details = null)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
