using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Commands;

public record ReservePetCommand : IRequest<ReservePetResponse>
{
    public ReservePetCommand(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public class ReservePetCommandHandler : IRequestHandler<ReservePetCommand, ReservePetResponse>
{
    private readonly IPetRepository _repo;
    private readonly IEventPublisher _eventPublisher;

    public ReservePetCommandHandler(IPetRepository repo, IEventPublisher eventPublisher)
    {
        _repo = repo;
        _eventPublisher = eventPublisher;
    }

    public async Task<ReservePetResponse> Handle(ReservePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repo.GetById(request.PetId);
        if (pet == null)
            return new ReservePetResponse(Success: false, Message: "Pet not found.");

        try
        {
            pet.Reserve();
            await _repo.Update(pet);

            await _eventPublisher.PublishAsync(pet.DomainEvents);
            pet.ClearDomainEvents();

            return new ReservePetResponse(
                Success: true,
                PetId: pet.Id,
                Status: pet.Status.ToString()
            );
        }
        catch (Exception ex)
        {
            return new ReservePetResponse(Success: false, Message: ex.Message);
        }
    }
}
