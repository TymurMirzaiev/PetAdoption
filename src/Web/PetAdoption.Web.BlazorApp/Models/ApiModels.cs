namespace PetAdoption.Web.BlazorApp.Models;

public record LoginRequest(string Email, string Password);
public record LoginResponse(bool Success, string Token, string RefreshToken, string UserId, string Email, string FullName, string Role, int ExpiresIn);
public record RegisterRequest(string Email, string FullName, string Password, string? PhoneNumber = null);
public record RegisterResponse(bool Success, string UserId, string Message);
public record GoogleAuthRequest(string IdToken);
public record GoogleAuthResponse(string AccessToken, string RefreshToken);
public record RefreshTokenRequest(string RefreshToken);
public record RefreshTokenResponse(string AccessToken, string RefreshToken);

public record PetListItem(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags = null, string? PrimaryPhotoUrl = null, int MediaCount = 0);
public record PetDetails(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description, List<string>? Tags = null, MedicalRecordModel? MedicalRecord = null, Guid? OrganizationId = null);
public record PetsResponse(IEnumerable<PetListItem> Pets, long Total);
public record DiscoverPetsResponse(IEnumerable<PetListItem> Pets, bool HasMore);
public record CreatePetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdatePetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);

public record CreateOrgPetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdateOrgPetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record OrgPetsResponse(IEnumerable<PetListItem> Pets, long Total, int Skip, int Take);

public record PetTypeItem(Guid Id, string Code, string Name, bool IsActive);
public record PetTypesResponse(IEnumerable<PetTypeItem> Items);

public record FavoriteItem(Guid FavoriteId, Guid PetId, string PetName, string PetType, string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
public record FavoritesResponse(IEnumerable<FavoriteItem> Items, long TotalCount, int Page, int PageSize);
public record CheckFavoriteResponse(bool IsFavorited);

public record AnnouncementListItem(Guid Id, string Title, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
public record AnnouncementDetail(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy, DateTime CreatedAt);
public record ActiveAnnouncement(Guid Id, string Title, string Body);
public record AnnouncementsResponse(IEnumerable<AnnouncementListItem> Items, long TotalCount);

public record UserProfile(string Id, string Email, string FullName, string? PhoneNumber, string Status, string Role, string? ExternalProvider, bool HasPassword, DateTime RegisteredAt, string? Bio = null);
public record UserListItem(string Id, string Email, string FullName, string Status, string Role, DateTime RegisteredAt);
public record UsersResponse(IEnumerable<UserListItem> Users, long TotalCount);

public record ApiError(string ErrorCode, string Message);

public record AdoptionRequestItem(
    Guid Id, Guid PetId, string PetName, string PetType, Guid OrganizationId,
    string Status, string? Message, string? RejectionReason,
    DateTime CreatedAt, DateTime? ReviewedAt);
public record AdoptionRequestsResponse(
    IEnumerable<AdoptionRequestItem> Items, long Total, int Skip, int Take);
public record OrgAdoptionRequestItem(
    Guid Id, Guid UserId, Guid PetId, string PetName,
    string Status, string? Message, DateTime CreatedAt);
public record OrgAdoptionRequestsResponse(
    IEnumerable<OrgAdoptionRequestItem> Items, long Total, int Skip, int Take);

public record PetMetricsSummaryItem(
    Guid PetId, string PetName, string PetType,
    long ImpressionCount, long SwipeCount, long RejectionCount,
    long FavoriteCount, double SwipeRate, double RejectionRate);
public record OrgMetricsResponse(IEnumerable<PetMetricsSummaryItem> Metrics);
public record PetMetricsDetailResponse(PetMetricsSummaryItem Metrics);

public record OrgDashboardResponse(int TotalPets, int AvailablePets, int ReservedPets, int AdoptedPets, int TotalAdoptionRequests, int PendingRequests, double AdoptionRate, long TotalImpressions, double AvgSwipeRate);
public record OrgDashboardTrendsResponse(IReadOnlyList<TrendPoint> AdoptionsByWeek, IReadOnlyList<TrendPoint> RequestsByWeek);
public record TrendPoint(DateTime WeekStart, string Label, int Count);

public record LocationDialogResult(decimal? Lat, decimal? Lng, int RadiusKm);

// Pet media models
public record PetMediaItem(Guid Id, string MediaType, string Url, string ContentType, int SortOrder, bool IsPrimary, DateTime CreatedAt);

// Pet medical record models
public record MedicalRecordModel(
    bool IsSpayedNeutered,
    DateOnly? SpayNeuterDate,
    string? MicrochipId,
    string? History,
    DateOnly? LastVetVisit,
    IReadOnlyList<VaccinationModel> Vaccinations,
    IReadOnlyList<string> Allergies,
    DateTime UpdatedAt);
public record VaccinationModel(string VaccineType, DateOnly AdministeredOn, DateOnly? NextDueOn, string? Notes);
public record UpdatePetMedicalRecordRequest(
    bool IsSpayedNeutered,
    DateOnly? SpayNeuterDate,
    string? MicrochipId,
    string? HistoryNotes,
    DateOnly? LastVetVisit,
    IReadOnlyList<VaccinationModel> Vaccinations,
    IReadOnlyList<string> Allergies);

public record ChatMessageDto(
    Guid Id, Guid AdoptionRequestId, Guid SenderUserId, string SenderRole,
    string Body, DateTime SentAt, DateTime? ReadByRecipientAt);

public record UpdateProfileRequest(string FullName, string? PhoneNumber, string? Bio);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record CreatePetTypeRequest(string Code, string Name);
public record UpdatePetTypeRequest(string Code, string Name);
public record CreateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
public record UpdateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
