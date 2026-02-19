namespace PetAdoption.UserService.Application.DTOs;

public record RegisterUserResponse(
    bool Success,
    string UserId,
    string Message
);
