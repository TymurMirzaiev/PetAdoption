using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record UpdateAnnouncementCommand(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate) : IRequest<UpdateAnnouncementResponse>;
