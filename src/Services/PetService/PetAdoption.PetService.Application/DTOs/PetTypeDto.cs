namespace PetAdoption.PetService.Application.DTOs;

/// <summary>
/// DTO representing a pet type.
/// </summary>
public record PetTypeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
