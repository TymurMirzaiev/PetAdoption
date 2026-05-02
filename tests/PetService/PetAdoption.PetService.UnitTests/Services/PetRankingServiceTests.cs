using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetAdoption.PetService.Application.Options;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Infrastructure.Persistence;
using PetAdoption.PetService.Infrastructure.Services;

namespace PetAdoption.PetService.UnitTests.Services;

public class PetRankingServiceTests : IDisposable
{
    private readonly PetServiceDbContext _db;
    private readonly PetRankingService _sut;
    private static readonly Guid PetTypeId = Guid.NewGuid();

    public PetRankingServiceTests()
    {
        var options = new DbContextOptionsBuilder<PetServiceDbContext>()
            .UseInMemoryDatabase(databaseName: $"PetRankingTest_{Guid.NewGuid():N}")
            .Options;
        _db = new PetServiceDbContext(options);

        var discoverOptions = Options.Create(new DiscoverOptions
        {
            FavoriteWeight = 1.0,
            SkipWeight = -0.5,
            PetTypeBonus = 0.10,
            AgeBucketBonus = 0.05
        });
        _sut = new PetRankingService(_db, discoverOptions);
    }

    public void Dispose() => _db.Dispose();

    private Pet CreatePet(Guid id, Guid? orgId = null, int? ageMonths = null, params string[] tags)
    {
        var pet = Pet.Create("Test", PetTypeId, null, ageMonths, null, tags);
        // Re-create with fixed ID by using reflection (domain model doesn't expose ID setter)
        // Instead we rely on EF tracking — just seed directly
        return pet;
    }

    // ──────────────────────────────────────────────────────────────
    // UserHasEnoughSignals
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserHasEnoughSignals_BelowThresholds_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // 4 favorites, 9 skips — both below thresholds
        for (var i = 0; i < 4; i++)
            _db.Favorites.Add(Favorite.Create(userId, Guid.NewGuid()));
        for (var i = 0; i < 9; i++)
            _db.PetSkips.Add(PetSkip.Create(userId, Guid.NewGuid()));
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UserHasEnoughSignalsAsync(userId, default);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasEnoughSignals_WithFiveFavorites_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            _db.Favorites.Add(Favorite.Create(userId, Guid.NewGuid()));
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UserHasEnoughSignalsAsync(userId, default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasEnoughSignals_WithTenSkips_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // 0 favorites, 10 skips
        for (var i = 0; i < 10; i++)
            _db.PetSkips.Add(PetSkip.Create(userId, Guid.NewGuid()));
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UserHasEnoughSignalsAsync(userId, default);

        // Assert
        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // RankAsync — empty candidates
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RankAsync_WithEmptyCandidates_ReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.RankAsync(userId, [], default);

        // Assert
        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // RankAsync — score ordering
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Score_WithMatchingTags_RanksHigherThanNonMatching()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Seed a favorited pet with tags "small" and "calm"
        var favPet = Pet.Create("FavPet", PetTypeId, null, null, null, ["small", "calm"]);
        _db.Pets.Add(favPet);
        _db.Favorites.Add(Favorite.Create(userId, favPet.Id));
        await _db.SaveChangesAsync();

        // Candidate A: matching tags
        var candidateA = Pet.Create("CandA", PetTypeId, null, null, null, ["small", "calm"]);
        // Candidate B: non-matching tags
        var candidateB = Pet.Create("CandB", PetTypeId, null, null, null, ["large", "energetic"]);

        // Act
        var ranked = await _sut.RankAsync(userId, [candidateA, candidateB], default);

        // Assert
        ranked.Should().HaveCount(2);
        ranked[0].Name.Value.Should().Be("CandA");
        ranked[1].Name.Value.Should().Be("CandB");
    }

    [Fact]
    public async Task Score_WithSkippedTagDominant_PenalisesCandidate()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Seed a skipped pet with tag "aggressive"
        var skippedPet = Pet.Create("SkippedPet", PetTypeId, null, null, null, ["aggressive"]);
        _db.Pets.Add(skippedPet);
        _db.PetSkips.Add(PetSkip.Create(userId, skippedPet.Id));
        await _db.SaveChangesAsync();

        // Candidate A: has the negatively-weighted tag
        var candidateA = Pet.Create("Aggressive", PetTypeId, null, null, null, ["aggressive"]);
        // Candidate B: neutral tags
        var candidateB = Pet.Create("Neutral", PetTypeId, null, null, null, ["calm"]);

        // Act
        var ranked = await _sut.RankAsync(userId, [candidateA, candidateB], default);

        // Assert — "Neutral" should rank higher because "aggressive" is penalised
        ranked.Should().HaveCount(2);
        ranked[0].Name.Value.Should().Be("Neutral");
    }

    [Fact]
    public async Task RankAsync_WithNoUserSignals_ReturnsCandidatesUnchanged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        // No favorites or skips seeded

        var candidateA = Pet.Create("A", PetTypeId, null, null, null, ["small"]);
        var candidateB = Pet.Create("B", PetTypeId, null, null, null, ["large"]);
        var candidates = new List<Pet> { candidateA, candidateB };

        // Act
        var result = await _sut.RankAsync(userId, candidates, default);

        // Assert — with no signals, cosine similarity is 0 for all; order determined by tie-breaking (stable sort)
        result.Should().HaveCount(2);
    }
}
