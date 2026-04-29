using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetAnnouncementByIdQuery(Guid Id) : IRequest<AnnouncementDetailDto>;
