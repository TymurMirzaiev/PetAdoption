using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetActiveAnnouncementsQuery() : IRequest<IEnumerable<ActiveAnnouncementDto>>;
