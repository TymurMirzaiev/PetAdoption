namespace PetAdoption.PetService.Domain;

using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

public class Announcement
{
    public Guid Id { get; private set; }
    public AnnouncementTitle Title { get; private set; } = null!;
    public AnnouncementBody Body { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Announcement() { }

    public static Announcement Create(string title, string body, DateTime startDate, DateTime endDate, Guid createdBy)
    {
        ValidateDates(startDate, endDate);

        return new Announcement
        {
            Id = Guid.NewGuid(),
            Title = new AnnouncementTitle(title),
            Body = new AnnouncementBody(body),
            StartDate = startDate,
            EndDate = endDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string body, DateTime startDate, DateTime endDate)
    {
        ValidateDates(startDate, endDate);
        Title = new AnnouncementTitle(title);
        Body = new AnnouncementBody(body);
        StartDate = startDate;
        EndDate = endDate;
    }

    private static void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new DomainException(PetDomainErrorCode.InvalidAnnouncementDates, "End date must be after start date.");
    }
}
