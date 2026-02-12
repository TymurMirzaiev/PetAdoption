namespace PetAdoption.PetService.Application.DTOs;

public record ReservePetResponse(
    bool Success,
    string? Message = null,
    Guid? PetId = null,
    string? Status = null
);
