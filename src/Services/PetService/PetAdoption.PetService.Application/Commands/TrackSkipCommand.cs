namespace PetAdoption.PetService.Application.Commands;

using PetAdoption.PetService.Application.Abstractions;

public record TrackSkipCommand(Guid UserId, Guid PetId) : IRequest<TrackSkipResponse>;
