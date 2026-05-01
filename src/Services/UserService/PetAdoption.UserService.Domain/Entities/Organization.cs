using System.Text.RegularExpressions;
using PetAdoption.UserService.Domain.Enums;

namespace PetAdoption.UserService.Domain.Entities;

public class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Organization() { }

    public static Organization Create(string name, string slug, string? description)
    {
        return Create(Guid.NewGuid(), name, slug, description);
    }

    public static Organization Create(Guid id, string name, string slug, string? description)
    {
        ValidateName(name);
        ValidateSlug(slug);

        return new Organization
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            Status = OrganizationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        ValidateName(name);
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == OrganizationStatus.Inactive)
            throw new InvalidOperationException("Organization is already inactive");
        Status = OrganizationStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == OrganizationStatus.Active)
            throw new InvalidOperationException("Organization is already active");
        Status = OrganizationStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
            throw new ArgumentException("Organization name must be between 2 and 100 characters.", nameof(name));
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));
        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length < 2 || trimmed.Length > 50)
            throw new ArgumentException("Slug must be between 2 and 50 characters.", nameof(slug));
        if (!SlugPattern.IsMatch(trimmed))
            throw new ArgumentException("Slug must be lowercase alphanumeric with hyphens only.", nameof(slug));
    }
}
