namespace PetAdoption.PetService.Application.Queries;

public interface IFavoriteQueryStore
{
    Task<(IEnumerable<FavoriteWithPetDto> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take);
}

public record FavoriteWithPetDto(
    Guid FavoriteId, Guid PetId, string PetName, string PetType,
    string? Breed, int? AgeMonths, string Status, DateTime CreatedAt);
