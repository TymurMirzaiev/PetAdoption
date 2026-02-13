using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Domain;

/// <summary>
/// Entity representing a pet type that can be managed by administrators.
/// Pet types have a lifecycle and can be created, updated, and deactivated.
/// </summary>
public class PetType : IEntity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique code for the pet type (e.g., "dog", "cat", "dragon").
    /// Used as a stable identifier in lowercase format.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Display name for the pet type (e.g., "Dog", "Cat", "Dragon").
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Indicates whether this pet type is active and can be used for new pets.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this pet type was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this pet type was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Private parameterless constructor for ORM/MongoDB deserialization
    private PetType() { }

    private PetType(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new pet type.
    /// </summary>
    public static PetType Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type code cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedCode", code ?? "null" }
                });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type name cannot be empty or whitespace.",
                new Dictionary<string, object>
                {
                    { "AttemptedName", name ?? "null" }
                });
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        var trimmedName = name.Trim();

        if (normalizedCode.Length < 2)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type code must be at least 2 characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedCode", code },
                    { "Length", normalizedCode.Length }
                });
        }

        if (normalizedCode.Length > 50)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type code cannot exceed 50 characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedCode", code },
                    { "Length", normalizedCode.Length }
                });
        }

        if (trimmedName.Length < 2 || trimmedName.Length > 100)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type name must be between 2 and 100 characters.",
                new Dictionary<string, object>
                {
                    { "AttemptedName", name },
                    { "Length", trimmedName.Length }
                });
        }

        return new PetType(Guid.NewGuid(), normalizedCode, trimmedName);
    }

    /// <summary>
    /// Updates the display name of the pet type.
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type name cannot be empty or whitespace.");
        }

        var trimmedName = newName.Trim();
        if (trimmedName.Length < 2 || trimmedName.Length > 100)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidPetType,
                "Pet type name must be between 2 and 100 characters.");
        }

        Name = trimmedName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the pet type, preventing it from being used for new pets.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                $"Pet type '{Code}' is already inactive.");
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a previously deactivated pet type.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException(
                PetDomainErrorCode.InvalidOperation,
                $"Pet type '{Code}' is already active.");
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
