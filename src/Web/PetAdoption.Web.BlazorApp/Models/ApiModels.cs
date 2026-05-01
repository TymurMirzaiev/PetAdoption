namespace PetAdoption.Web.BlazorApp.Models;

public record LoginRequest(string Email, string Password);
public record LoginResponse(bool Success, string Token, string RefreshToken, string UserId, string Email, string FullName, string Role, int ExpiresIn);
public record RegisterRequest(string Email, string FullName, string Password, string? PhoneNumber = null);
public record RegisterResponse(bool Success, string UserId, string Message);
public record GoogleAuthRequest(string IdToken);
public record GoogleAuthResponse(string AccessToken, string RefreshToken);
public record RefreshTokenRequest(string RefreshToken);
public record RefreshTokenResponse(string AccessToken, string RefreshToken);

public record PetListItem(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);
public record PetDetails(Guid Id, string Name, string Type, string Status, string? Breed, int? AgeMonths, string? Description);
public record PetsResponse(IEnumerable<PetListItem> Pets, long Total);
public record CreatePetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null);
public record UpdatePetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null);

public record PetTypeItem(Guid Id, string Code, string Name, bool IsActive);
public record PetTypesResponse(IEnumerable<PetTypeItem> Items);

public record FavoriteItem(Guid FavoriteId, Guid PetId, string PetName, string PetType, string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
public record FavoritesResponse(IEnumerable<FavoriteItem> Items, long TotalCount, int Page, int PageSize);
public record CheckFavoriteResponse(bool IsFavorited);

public record AnnouncementListItem(Guid Id, string Title, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
public record AnnouncementDetail(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy, DateTime CreatedAt);
public record ActiveAnnouncement(Guid Id, string Title, string Body);
public record AnnouncementsResponse(IEnumerable<AnnouncementListItem> Items, long TotalCount);

public record UserProfile(string Id, string Email, string FullName, string? PhoneNumber, string Status, string Role, string? ExternalProvider, bool HasPassword, DateTime RegisteredAt);
public record UserListItem(string Id, string Email, string FullName, string Status, string Role, DateTime RegisteredAt);
public record UsersResponse(IEnumerable<UserListItem> Users, long TotalCount);

public record ApiError(string ErrorCode, string Message);
