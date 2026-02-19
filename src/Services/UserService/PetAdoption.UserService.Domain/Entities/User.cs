namespace PetAdoption.UserService.Domain.Entities;

using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Events;

public class User
{
    public UserId Id { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public FullName FullName { get; private set; } = null!;
    public Password Password { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public UserPreferences Preferences { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private readonly List<DomainEventBase> _domainEvents = new();
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for MongoDB/EF Core
    private User() { }

    /// <summary>
    /// Factory method for creating a new user (registration)
    /// </summary>
    public static User Register(
        string email,
        string fullName,
        string hashedPassword,  // Already hashed by infrastructure
        string? phoneNumber = null,
        UserRole role = UserRole.User)
    {
        var user = new User
        {
            Id = UserId.Create(),
            Email = Email.From(email),
            FullName = FullName.From(fullName),
            Password = Password.FromHash(hashedPassword),
            Role = role,
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber),
            Preferences = UserPreferences.Default(),
            Status = UserStatus.Active,
            RegisteredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id.Value,
            user.Email.Value,
            user.FullName.Value,
            user.Role.ToString(),
            user.RegisteredAt
        ));

        return user;
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public void UpdateProfile(
        string? fullName = null,
        string? phoneNumber = null,
        UserPreferences? preferences = null)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("Cannot update profile of suspended user");

        var hasChanges = false;

        if (fullName != null)
        {
            FullName = FullName.From(fullName);
            hasChanges = true;
        }

        if (phoneNumber != null)
        {
            PhoneNumber = PhoneNumber.FromOptional(phoneNumber);
            hasChanges = true;
        }

        if (preferences != null)
        {
            Preferences = preferences;
            hasChanges = true;
        }

        if (hasChanges)
        {
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserProfileUpdatedEvent(
                Id.Value,
                fullName,
                phoneNumber,
                UpdatedAt
            ));
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public void ChangePassword(string newHashedPassword)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("Cannot change password of suspended user");

        Password = Password.FromHash(newHashedPassword);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedEvent(
            Id.Value,
            UpdatedAt
        ));
    }

    /// <summary>
    /// Promote user to admin role
    /// </summary>
    public void PromoteToAdmin()
    {
        if (Role == UserRole.Admin)
            throw new InvalidOperationException("User is already an admin");

        Role = UserRole.Admin;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserRoleChangedEvent(
            Id.Value,
            UserRole.Admin.ToString(),
            UpdatedAt
        ));
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        // Don't raise event for login (too noisy), just track timestamp
    }

    /// <summary>
    /// Suspend user account
    /// </summary>
    public void Suspend(string reason)
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("User is already suspended");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserSuspendedEvent(
            Id.Value,
            reason,
            UpdatedAt
        ));
    }

    /// <summary>
    /// Activate suspended user account
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear domain events (after publishing)
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(DomainEventBase @event)
    {
        _domainEvents.Add(@event);
    }
}
