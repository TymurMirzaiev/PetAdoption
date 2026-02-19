namespace PetAdoption.UserService.Application.DTOs;

public record LoginResponse(
    bool Success,
    string Token,
    string UserId,
    string Email,
    string FullName,
    string Role,
    int ExpiresIn  // Seconds until token expires
);
