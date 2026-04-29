using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAnnouncementResponse(Guid Id);

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, CreateAnnouncementResponse>
{
    private readonly IAnnouncementRepository _repository;

    public CreateAnnouncementCommandHandler(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateAnnouncementResponse> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken = default)
    {
        var announcement = Announcement.Create(request.Title, request.Body, request.StartDate, request.EndDate, request.CreatedBy);
        await _repository.AddAsync(announcement);
        return new CreateAnnouncementResponse(announcement.Id);
    }
}
