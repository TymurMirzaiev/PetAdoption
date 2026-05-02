namespace PetAdoption.UserService.IntegrationTests.Helpers;

internal record LoginResponseDto(
    bool Success,
    string Token,
    string UserId,
    string Email,
    string FullName,
    string Role,
    int ExpiresIn);

internal record RegisterResponse(
    bool Success,
    string UserId,
    string Email,
    string FullName,
    string Role);
