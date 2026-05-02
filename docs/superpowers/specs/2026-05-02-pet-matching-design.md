# Pet Matching Algorithm — Design Spec

**Date:** 2026-05-02
**Status:** Approved
**Scope:** PetService backend only — replace random ordering in `GetDiscoverPetsQueryHandler` with a personalised tag-vector ranker. Feature-flagged with random fallback. No API contract change, no frontend change, no schema change.

---

## Overview

Discover currently returns `Available` pets ordered by `Pet.Id` (effectively random because GUIDs are uniformly distributed). Every signal we need for personalisation already exists: favourites (`Favorite`), skips (`PetSkip`), impressions/swipes (`PetInteraction`), and a normalised `PetTag` value object on every pet. The key insight is that we can build a meaningful tag-based preference profile per user **on demand from existing tables** — no new domain entity, no new background job, no embedding model. We compute a sparse tag vector for the user, score a bounded candidate set by cosine similarity plus a couple of cheap bonuses, and re-rank. Anything unproven about the relevance of these signals is hidden behind `Discover:RankingEnabled` so we can flip back to the current random feed instantly.

---

## Algorithm sketch

Three bullets, no math walls:

- **User vector** — fetch the user's favourited pets (weight `+1.0`) and skipped pets (weight `−0.5`); for each pet sum its tag values into a `Dictionary<string, double>`. The result is a sparse preference vector across the same `PetTag` vocabulary the catalogue already uses.
- **Candidate score** — for each candidate pet build a tag vector (each tag weight = 1), then `score = cosine(userVec, candidateVec) + 0.10 * petTypeMatchRatio + 0.05 * ageBucketMatchRatio`. The two bonuses come from the dominant `PetTypeId` and the dominant 12-month age bucket among the user's favourites; no bonus when the user has no favourites of that type/age.
- **Bounded re-rank** — fetch the existing top `Take * 10` (capped at 100) `Available` candidates from `IPetQueryStore.GetDiscoverable`, score them in memory in the handler's process, sort by score descending, return the top `Take + 1` to preserve the current `HasMore` contract. We never score the full `Pets` table.

---

## Application changes

### New service: `IPetRankingService`

**File:** `Application/Services/IPetRankingService.cs` + `Infrastructure/Services/PetRankingService.cs`
**Lifetime:** Scoped (uses scoped repositories + `PetServiceDbContext`)
**Interface:**

```csharp
public interface IPetRankingService
{
    Task<bool> UserHasEnoughSignalsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Pet>> RankAsync(Guid userId, IReadOnlyList<Pet> candidates, CancellationToken ct);
}
```

`UserHasEnoughSignalsAsync` returns `true` once the user has `≥ 5` favourites OR `≥ 10` total swipes (favourites + skips). `RankAsync` is a pure in-memory transform once the user vector is built — no DB calls per candidate.

### Handler change: `GetDiscoverPetsQueryHandler`

```
1. Fetch skipped + favourited IDs (unchanged)
2. Read Discover:RankingEnabled from IOptions<DiscoverOptions>
3. If disabled OR !UserHasEnoughSignalsAsync → fetch Take+1 candidates as today, return random order
4. Else → fetch Min(Take * 10, 100) candidates, RankAsync, take Take+1, return ranked
5. Map to DTOs (unchanged)
```

`HasMore` semantics stay identical: handler still over-fetches by 1 from the *post-rank* slice. The DTO shape (`GetDiscoverPetsResponse`) is unchanged.

### Configuration

```json
"Discover": {
  "RankingEnabled": true,
  "CandidatePoolMultiplier": 10,
  "CandidatePoolCap": 100,
  "FavoriteWeight": 1.0,
  "SkipWeight": -0.5,
  "PetTypeBonus": 0.10,
  "AgeBucketBonus": 0.05
}
```

Bound to `DiscoverOptions` and registered in `Program.cs`. Defaults baked into the options class so missing config doesn't crash. `RankingEnabled` defaults to `false` — explicit opt-in per environment.

### Query store change

`IPetQueryStore.GetDiscoverable` gains an optional `int? candidatePoolSize` parameter (defaults to `take` to preserve callers). When the ranker is on, the handler passes the larger pool. Ordering inside the store stays `OrderBy(p => p.Id)` — randomness of the candidate pool is fine; the ranker provides the meaningful order.

---

## Domain / data changes

**None.** Everything reads from existing tables (`Favorites`, `PetSkips`, `Pets.Tags`). No migration. No new aggregate. No new domain event. The `PetInteraction` table exists and could be incorporated later as a lower-weight neutral signal, but is **out of scope** for this iteration to keep the user vector cheap (favourites + skips are bounded per user, impressions are not).

---

## Frontend changes

**None.** `GET /api/pets/discover` keeps the same request and response shape; only the order of items in `Pets` changes. The Blazor swipe UI consumes the list as-is. No new client model, no new endpoint, no flag plumbing on the frontend.

---

## Performance notes

- User vector computation: 1 query for favourite pet IDs (already cached in handler), 1 query for skipped pet IDs (already cached), 1 batch query `_db.Pets.Where(p => idList.Contains(p.Id)).Select(p => new { p.Id, p.PetTypeId, p.Age, p.Tags })` to get tags for both lists in a single round-trip.
- Candidate scoring: in-memory, O(candidates × avgTagsPerPet). With pool cap 100 and ~5 tags/pet this is ~500 dictionary lookups — negligible.
- No caching of user vectors initially. If profiling shows cost, add an `IMemoryCache` keyed by `userId` with a 5-minute TTL invalidated on favourite/skip writes — out of scope for v1.

---

## Out of scope

- ML models, sentence embeddings, ANN/vector DBs (pgvector, Pinecone, etc.)
- Collaborative filtering / "users who liked X also liked Y"
- A/B testing infrastructure (we have a single binary feature flag, nothing more)
- Click-through learning from `PetInteraction.Impression`
- Persisted preference profiles, background recompute jobs
- Cross-user popularity boost ("trending pets")
- Frontend signals (dwell time, partial swipe direction)

---

## Testing

**Unit tests** (`PetService.UnitTests/Services/PetRankingServiceTests.cs`):
- `Score_WithMatchingTags_RanksHigherThanNonMatching` — fixed user vector, two candidates, deterministic order
- `Score_WithSkippedTagDominant_PenalisesCandidate` — skip weight pulls score down
- `UserHasEnoughSignalsAsync_BelowThresholds_ReturnsFalse` — 4 favourites + 9 swipes → false; 5 favourites → true; 10 swipes → true
- `RankAsync_WithEmptyCandidates_ReturnsEmpty` — boundary
- `RankAsync_WithNoUserSignals_ReturnsCandidatesUnchanged` — defensive (handler should gate this, but ranker stays safe)

**Integration tests** (`PetService.IntegrationTests/Discover/DiscoverRankingTests.cs`):
- `Discover_WithFlagOff_ReturnsRandomOrdering` — assert order matches the existing random behaviour
- `Discover_WithStrongFavouriteSignal_PrioritisesMatchingTags` — seed user with 5 favourites all tagged `["small", "calm"]`, seed 10 candidates split between matching and non-matching tags, assert top results are matching pets
- `Discover_WithColdStartUser_FallsBackToRandom` — user with 0 favourites, flag on → handler skips ranker
- `Discover_HasMoreContract_PreservedWhenRanked` — pool produces > Take results, `HasMore == true`

Use the existing `PetServiceWebAppFactory`; flip `Discover:RankingEnabled` per test via `WithWebHostBuilder(b => b.ConfigureAppConfiguration(...))`.
