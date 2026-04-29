using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Commands;

public record CreateAnnouncementCommand(string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy) : IRequest<CreateAnnouncementResponse>;
