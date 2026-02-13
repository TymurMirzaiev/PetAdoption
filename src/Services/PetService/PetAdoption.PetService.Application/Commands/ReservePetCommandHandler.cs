using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record ReservePetCommand : IRequest<ReservePetResponse>
{
    public ReservePetCommand(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public record ReservePetResponse(
    bool Success,
    string? Message = null,
    Guid? PetId = null,
    string? Status = null
);

public class ReservePetCommandHandler : IRequestHandler<ReservePetCommand, ReservePetResponse>
{
    private readonly IPetRepository _repository;

    public ReservePetCommandHandler(IPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservePetResponse> Handle(ReservePetCommand request, CancellationToken cancellationToken = default)
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

        pet.Reserve();
        await _repository.Update(pet);

        return new ReservePetResponse(
            Success: true,
            PetId: pet.Id,
            Status: pet.Status.ToString()
        );
    }
}
