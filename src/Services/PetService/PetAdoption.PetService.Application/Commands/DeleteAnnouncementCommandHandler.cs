using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record DeleteAnnouncementResponse(bool Success);

public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand, DeleteAnnouncementResponse>
{
    private readonly IAnnouncementRepository _repository;

    public DeleteAnnouncementCommandHandler(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteAnnouncementResponse> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken = default)
    {
        var announcement = await _repository.GetByIdAsync(request.Id)
            ?? throw new DomainException(PetDomainErrorCode.AnnouncementNotFound, $"Announcement {request.Id} not found.");

        await _repository.DeleteAsync(request.Id);
        return new DeleteAnnouncementResponse(true);
    }
}
