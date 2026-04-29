using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Application.Queries;

public record GetAnnouncementsQuery(int Skip, int Take) : IRequest<GetAnnouncementsResponse>;
