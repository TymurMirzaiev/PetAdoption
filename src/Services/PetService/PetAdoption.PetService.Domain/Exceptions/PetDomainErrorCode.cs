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
    /// Pet cannot be deleted because it is reserved or adopted.
    /// </summary>
    public const string PetCannotBeDeleted = "pet_cannot_be_deleted";

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
    /// Pet breed is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidPetBreed = "invalid_pet_breed";

    /// <summary>
    /// Pet type is invalid (not in allowed list).
    /// </summary>
    public const string InvalidPetType = "invalid_pet_type";

    /// <summary>
    /// Pet age is invalid (negative value).
    /// </summary>
    public const string InvalidPetAge = "invalid_pet_age";

    /// <summary>
    /// Pet description is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidPetDescription = "invalid_pet_description";

    /// <summary>
    /// Pet tag is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidPetTag = "invalid_pet_tag";

    /// <summary>
    /// Pet type with the given code already exists.
    /// </summary>
    public const string PetTypeAlreadyExists = "pet_type_already_exists";

    /// <summary>
    /// Pet type was not found.
    /// </summary>
    public const string PetTypeNotFound = "pet_type_not_found";

    // General domain errors

    /// <summary>
    /// Invalid operation or business rule violation.
    /// </summary>
    public const string InvalidOperation = "invalid_operation";

    /// <summary>
    /// Unknown domain error.
    /// </summary>
    public const string UnknownDomainError = "unknown_domain_error";

    // Favorite errors

    /// <summary>
    /// Pet is already in the user's favorites.
    /// </summary>
    public const string FavoriteAlreadyExists = "favorite_already_exists";

    /// <summary>
    /// Favorite was not found.
    /// </summary>
    public const string FavoriteNotFound = "favorite_not_found";

    // Skip errors

    /// <summary>
    /// Pet is already in the user's skips.
    /// </summary>
    public const string SkipAlreadyExists = "skip_already_exists";

    // Announcement errors

    /// <summary>
    /// Announcement title is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidAnnouncementTitle = "invalid_announcement_title";

    /// <summary>
    /// Announcement body is invalid (empty, too long, etc.).
    /// </summary>
    public const string InvalidAnnouncementBody = "invalid_announcement_body";

    /// <summary>
    /// Announcement dates are invalid (end date not after start date).
    /// </summary>
    public const string InvalidAnnouncementDates = "invalid_announcement_dates";

    /// <summary>
    /// Announcement was not found.
    /// </summary>
    public const string AnnouncementNotFound = "announcement_not_found";

    // Adoption request errors

    /// <summary>
    /// Adoption request was not found.
    /// </summary>
    public const string AdoptionRequestNotFound = "adoption_request_not_found";

    /// <summary>
    /// Adoption request is not pending and cannot transition.
    /// </summary>
    public const string AdoptionRequestNotPending = "adoption_request_not_pending";

    /// <summary>
    /// A pending adoption request already exists for this user/pet pair.
    /// </summary>
    public const string AdoptionRequestAlreadyExists = "adoption_request_already_exists";

    /// <summary>
    /// Rejection reason is invalid (empty or whitespace).
    /// </summary>
    public const string InvalidRejectionReason = "invalid_rejection_reason";
}
