using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record DeleteAnnouncementCommand(Guid Id) : IRequest<DeleteAnnouncementResponse>;
