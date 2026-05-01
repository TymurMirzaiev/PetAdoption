namespace PetAdoption.PetService.Application.Queries;

public record PetMetricsSummary(
    Guid PetId,
    string PetName,
    string PetType,
    long ImpressionCount,
    long SwipeCount,
    long RejectionCount,
    long FavoriteCount,
    double SwipeRate,
    double RejectionRate);

public interface IPetMetricsQueryStore
{
    Task<IEnumerable<PetMetricsSummary>> GetMetricsByOrgAsync(
        Guid orgId, DateTime? from, DateTime? to, string? sortBy, bool descending);
    Task<PetMetricsSummary?> GetMetricsByPetAsync(Guid petId, DateTime? from, DateTime? to);
}
