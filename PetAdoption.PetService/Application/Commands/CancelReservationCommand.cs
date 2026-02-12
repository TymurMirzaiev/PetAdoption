using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Application.Commands;

public record CancelReservationCommand : IRequest<CancelReservationResponse>
{
    public CancelReservationCommand(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public record CancelReservationResponse(
    bool Success,
    string? Message = null,
    Guid? PetId = null,
    string? Status = null
);

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, CancelReservationResponse>
{
    private readonly IPetRepository _repository;

    public CancelReservationCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<CancelReservationResponse> Handle(CancelReservationCommand request, CancellationToken cancellationToken = default)
    {
        var pet = await _repository.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        pet.CancelReservation();
        await _repository.Update(pet);

        return new CancelReservationResponse(
            Success: true,
            PetId: pet.Id,
            Status: pet.Status.ToString()
        );
    }
}
