using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Infrastructure;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Commands;

public record ReservePetCommand : IRequest<ReservePetCommandResult>
{
    public ReservePetCommand(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public class ReservePetCommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class ReservePetCommandHandler : IRequestHandler<ReservePetCommand, ReservePetCommandResult>
{
    private readonly IPetRepository _repo;
    private readonly IEventPublisher _eventPublisher;

    public ReservePetCommandHandler(IPetRepository repo, IEventPublisher eventPublisher)
    {
        _repo = repo;
        _eventPublisher = eventPublisher;
    }

    public async Task<ReservePetCommandResult> Handle(ReservePetCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repo.GetById(request.PetId); // Use async method
        if (pet == null)
            return new ReservePetCommandResult { Success = false, Message = "Pet not found." };

        try
        {
            pet.Reserve();
            await _repo.Update(pet);

            await _eventPublisher.PublishAsync(pet.DomainEvents);

            return new ReservePetCommandResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ReservePetCommandResult { Success = false, Message = ex.Message };
        }
    }
}
