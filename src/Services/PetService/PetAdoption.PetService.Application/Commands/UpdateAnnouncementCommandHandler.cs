using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdateAnnouncementResponse(Guid Id);

public class UpdateAnnouncementCommandHandler : IRequestHandler<UpdateAnnouncementCommand, UpdateAnnouncementResponse>
{
    private readonly IAnnouncementRepository _repository;

    public UpdateAnnouncementCommandHandler(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateAnnouncementResponse> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken = default)
    {
        var announcement = await _repository.GetByIdAsync(request.Id)
            ?? throw new DomainException(PetDomainErrorCode.AnnouncementNotFound, $"Announcement {request.Id} not found.");

        announcement.Update(request.Title, request.Body, request.StartDate, request.EndDate);
        await _repository.UpdateAsync(announcement);
        return new UpdateAnnouncementResponse(announcement.Id);
    }
}
