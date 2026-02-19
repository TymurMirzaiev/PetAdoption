namespace PetAdoption.UserService.Application.DTOs;

public record UserListItemDto(
    string Id,
    string Email,
    string FullName,
    string Status,
    string Role,
    DateTime RegisteredAt
);
