namespace PetAdoption.PetService.Domain;

public enum InteractionType
{
    Impression = 0,
    Swipe = 1,
    Rejection = 2
}

public class PetInteraction
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid UserId { get; private set; }
    public InteractionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PetInteraction() { }

    public static PetInteraction Create(Guid petId, Guid userId, InteractionType type)
    {
        if (petId == Guid.Empty)
            throw new ArgumentException("PetId cannot be empty.", nameof(petId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new PetInteraction
        {
            Id = Guid.NewGuid(),
            PetId = petId,
            UserId = userId,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }
}
