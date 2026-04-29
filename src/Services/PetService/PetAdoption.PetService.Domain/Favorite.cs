namespace PetAdoption.PetService.Domain;

public class Favorite
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PetId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Favorite() { }

    public static Favorite Create(Guid userId, Guid petId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId cannot be empty.", nameof(petId));

        return new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PetId = petId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
