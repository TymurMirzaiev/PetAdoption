namespace PetAdoption.PetService.Application.Queries;

using PetAdoption.PetService.Application.Abstractions;

public record GetFavoritesQuery(
    Guid UserId,
    int Skip,
    int Take,
    Guid? PetTypeId = null,
    string? PetStatus = null,
    string SortBy = "newest") : IRequest<GetFavoritesResponse>;
