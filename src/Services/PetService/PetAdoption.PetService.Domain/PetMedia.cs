namespace PetAdoption.PetService.Domain;

public class PetMedia
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public PetMediaType MediaType { get; private set; }
    public string Url { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PetMedia() { }

    internal static PetMedia CreatePhoto(Guid petId, Guid id, string url, string contentType, int sortOrder) =>
        new()
        {
            Id = id,
            PetId = petId,
            MediaType = PetMediaType.Photo,
            Url = url,
            ContentType = contentType,
            SortOrder = sortOrder,
            IsPrimary = false,
            CreatedAt = DateTime.UtcNow
        };

    internal static PetMedia CreateVideo(Guid petId, Guid id, string url, string contentType) =>
        new()
        {
            Id = id,
            PetId = petId,
            MediaType = PetMediaType.Video,
            Url = url,
            ContentType = contentType,
            SortOrder = 0,
            IsPrimary = false,
            CreatedAt = DateTime.UtcNow
        };

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    internal void SetSortOrder(int order) => SortOrder = order;
}
