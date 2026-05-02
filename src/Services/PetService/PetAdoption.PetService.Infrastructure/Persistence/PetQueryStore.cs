using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetQueryStore : IPetQueryStore
{
    private readonly PetServiceDbContext _db;

    public PetQueryStore(PetServiceDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Pet>> GetAll()
    {
        return await _db.Pets.AsNoTracking().ToListAsync();
    }

    public async Task<Pet?> GetById(Guid id)
    {
        return await _db.Pets
            .AsNoTracking()
            .Include("_media")
            .Include(p => p.MedicalRecord)
            .ThenInclude(mr => mr!.Vaccinations)
            .Include(p => p.MedicalRecord)
            .ThenInclude(mr => mr!.Allergies)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Pet>> GetByIds(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _db.Pets.AsNoTracking()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<(IEnumerable<Pet> Pets, long Total)> GetFiltered(
        PetStatus? status,
        Guid? petTypeId,
        int skip,
        int take,
        int? minAgeMonths = null,
        int? maxAgeMonths = null,
        string? breedSearch = null,
        IEnumerable<string>? tags = null)
    {
        var query = _db.Pets.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (petTypeId.HasValue)
            query = query.Where(p => p.PetTypeId == petTypeId.Value);

        if (minAgeMonths.HasValue)
        {
            var minAge = new PetAge(minAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age >= minAge);
        }

        if (maxAgeMonths.HasValue)
        {
            var maxAge = new PetAge(maxAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age <= maxAge);
        }

        if (!string.IsNullOrWhiteSpace(breedSearch))
        {
            var trimmed = breedSearch.Trim();
            query = query.Where(p => p.Breed != null && EF.Functions.Like((string)p.Breed, $"%{trimmed}%"));
        }

        query = await ApplyTagFilterAsync(query, tags);

        var total = await query.LongCountAsync();
        var pets = await query.OrderBy(p => p.Name).Skip(skip).Take(take).ToListAsync();

        return (pets, total);
    }

    public async Task<(IEnumerable<Pet> Pets, long Total)> GetDiscoverable(
        HashSet<Guid> excludedPetIds,
        Guid? petTypeId,
        int? minAgeMonths,
        int? maxAgeMonths,
        int take,
        string? breedSearch = null,
        decimal? lat = null,
        decimal? lng = null,
        int? radiusKm = null,
        int? candidatePoolSize = null)
    {
        var query = _db.Pets.AsNoTracking()
            .Where(p => p.Status == PetStatus.Available);

        if (excludedPetIds.Count > 0)
            query = query.Where(p => !excludedPetIds.Contains(p.Id));

        if (petTypeId.HasValue)
            query = query.Where(p => p.PetTypeId == petTypeId.Value);

        if (minAgeMonths.HasValue)
        {
            var minAge = new PetAge(minAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age >= minAge);
        }

        if (maxAgeMonths.HasValue)
        {
            var maxAge = new PetAge(maxAgeMonths.Value);
            query = query.Where(p => p.Age != null && p.Age <= maxAge);
        }

        if (!string.IsNullOrWhiteSpace(breedSearch))
        {
            var trimmed = breedSearch.Trim();
            query = query.Where(p => p.Breed != null && EF.Functions.Like((string)p.Breed, $"%{trimmed}%"));
        }

        // Location filter: bounding-box pre-filter on Organizations, then Haversine fine-filter in memory
        if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
        {
            var latRange = radiusKm.Value / 111.0m;
            var lngRange = radiusKm.Value / (111.0m * (decimal)Math.Cos((double)(lat.Value * (decimal)Math.PI / 180)));

            var orgIds = await _db.Organizations
                .Where(o => o.Address != null
                    && o.Address.Lat >= lat.Value - latRange && o.Address.Lat <= lat.Value + latRange
                    && o.Address.Lng >= lng.Value - lngRange && o.Address.Lng <= lng.Value + lngRange)
                .Select(o => new { o.Id, o.Address!.Lat, o.Address.Lng })
                .ToListAsync();

            var radiusKmDbl = (double)radiusKm.Value;
            var latDbl = (double)lat.Value;
            var lngDbl = (double)lng.Value;
            const double R = 6371;

            var nearbyOrgIds = orgIds.Where(o =>
            {
                var dlat = ((double)o.Lat - latDbl) * Math.PI / 180;
                var dlng = ((double)o.Lng - lngDbl) * Math.PI / 180;
                var a = Math.Sin(dlat / 2) * Math.Sin(dlat / 2)
                    + Math.Cos(latDbl * Math.PI / 180) * Math.Cos((double)o.Lat * Math.PI / 180)
                    * Math.Sin(dlng / 2) * Math.Sin(dlng / 2);
                return 2 * R * Math.Asin(Math.Sqrt(a)) <= radiusKmDbl;
            }).Select(o => o.Id).ToHashSet();

            query = query.Where(p => p.OrganizationId.HasValue && nearbyOrgIds.Contains(p.OrganizationId.Value));
        }

        var total = await query.LongCountAsync();
        // Order by Id (Guid.NewGuid is uniformly distributed) for a deterministic-yet-varied feed.
        var finalTake = candidatePoolSize ?? take;
        var pets = await query.OrderBy(p => p.Id).Take(finalTake).ToListAsync();

        return (pets, total);
    }

    public async Task<(IEnumerable<Pet> Pets, long Total)> GetFilteredByOrg(
        Guid organizationId,
        PetStatus? status,
        int skip,
        int take,
        IEnumerable<string>? tags = null)
    {
        var query = _db.Pets.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        query = await ApplyTagFilterAsync(query, tags, organizationId);

        var total = await query.LongCountAsync();
        var pets = await query.OrderBy(p => p.Name).Skip(skip).Take(take).ToListAsync();

        return (pets, total);
    }

    /// <summary>
    /// Pushes tag filtering down to SQL Server via OPENJSON, returning a query restricted to
    /// pets that contain ALL specified tag values. EF Core can't translate List queries against
    /// the JSON-converted Tags column, so we resolve matching ids in a single OPENJSON query
    /// and chain the result back into the LINQ pipeline.
    /// When organizationId is provided, the raw SQL is pre-filtered by that org to avoid a
    /// full-table scan on the Tags JSON column.
    /// </summary>
    private async Task<IQueryable<Pet>> ApplyTagFilterAsync(
        IQueryable<Pet> query, IEnumerable<string>? tags, Guid? organizationId = null)
    {
        if (tags is null) return query;

        var tagList = tags.Select(t => t.Trim().ToLowerInvariant()).ToList();
        if (tagList.Count == 0) return query;

        var sql = new StringBuilder("SELECT Id FROM Pets WHERE Tags IS NOT NULL");
        var parameters = new List<SqlParameter>();

        if (organizationId.HasValue)
        {
            sql.Append(" AND OrganizationId = @orgId");
            parameters.Add(new SqlParameter("@orgId", organizationId.Value));
        }

        for (var i = 0; i < tagList.Count; i++)
        {
            sql.Append($" AND EXISTS (SELECT 1 FROM OPENJSON(Tags) WHERE [value] = @t{i})");
            parameters.Add(new SqlParameter($"@t{i}", tagList[i]));
        }

        var matchingIds = await _db.Database
            .SqlQueryRaw<Guid>(sql.ToString(), parameters.Cast<object>().ToArray())
            .ToListAsync();

        return query.Where(p => matchingIds.Contains(p.Id));
    }
}
