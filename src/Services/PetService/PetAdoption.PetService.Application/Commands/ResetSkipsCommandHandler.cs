namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

public record ResetSkipsResponse(bool Success);

public class ResetSkipsCommandHandler : IRequestHandler<ResetSkipsCommand, ResetSkipsResponse>
{
    private readonly IPetSkipRepository _skipRepository;

    public ResetSkipsCommandHandler(IPetSkipRepository skipRepository)
    {
        _skipRepository = skipRepository;
    }

    public async Task<ResetSkipsResponse> Handle(ResetSkipsCommand request, CancellationToken ct)
    {
        await _skipRepository.DeleteAllByUserAsync(request.UserId);
        return new ResetSkipsResponse(true);
    }
}
