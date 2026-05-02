namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

public class ResetSkipsCommandHandler : IRequestHandler<ResetSkipsCommand, Unit>
{
    private readonly IPetSkipRepository _skipRepository;

    public ResetSkipsCommandHandler(IPetSkipRepository skipRepository)
    {
        _skipRepository = skipRepository;
    }

    public async Task<Unit> Handle(ResetSkipsCommand request, CancellationToken ct)
    {
        await _skipRepository.DeleteAllByUserAsync(request.UserId);
        return Unit.Value;
    }
}
