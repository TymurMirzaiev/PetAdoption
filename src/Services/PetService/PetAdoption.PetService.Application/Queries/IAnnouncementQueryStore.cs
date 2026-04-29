namespace PetAdoption.PetService.Application.Queries;

public interface IAnnouncementQueryStore
{
    Task<(IEnumerable<AnnouncementListDto> Items, long Total)> GetAllAsync(int skip, int take);
    Task<AnnouncementDetailDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ActiveAnnouncementDto>> GetActiveAsync();
}

public record AnnouncementListDto(Guid Id, string Title, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
public record AnnouncementDetailDto(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy, DateTime CreatedAt);
public record ActiveAnnouncementDto(Guid Id, string Title, string Body);
