namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;

public record GetFavoritesQuery(Guid UserId, int Skip, int Take) : IRequest<GetFavoritesResponse>;
