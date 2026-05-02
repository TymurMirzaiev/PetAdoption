# Pet Photos & Video — Design Spec

**Date:** 2026-05-02  
**Status:** Draft  
**Scope:** PetService backend (new aggregate-owned media + storage abstraction + 5 endpoints) + Blazor WASM frontend (gallery component + updates to swipe card, pet detail, org grid, pet form)

---

## Overview

Each pet can have multiple photos and an optional short video (≤30s) attached. Photos are publicly viewable everywhere a pet is shown (Discover swipe cards, pet detail page, Org Pets data grid thumbnail, admin pages). Upload, delete, reorder, and primary-photo selection are restricted to the owning organisation's Admin/Moderator. Binary storage is hidden behind an `IMediaStorage` abstraction so dev (Azurite or local disk) and prod (cloud blob storage) implementations can swap in without touching domain or application layers.

---

## Domain changes

`PetMedia` is modelled as an **owned collection on the `Pet` aggregate** (not a separate aggregate). Pet is the consistency boundary: media count, primary-photo invariant, and 1-video cap are enforced together with the pet's lifecycle.

```csharp
public class PetMedia
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }            // back-reference for queries
    public PetMediaType MediaType { get; private set; } // Photo | Video
    public string Url { get; private set; }            // public URL produced by IMediaStorage
    public string ContentType { get; private set; }    // e.g. "image/jpeg", "video/mp4"
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

New methods on `Pet`:
- `AddPhoto(Guid mediaId, string url, string contentType)` — appends at end; first photo auto-set as primary.
- `AddVideo(Guid mediaId, string url, string contentType)` — throws `DomainException(invalid_operation)` if a video already exists.
- `RemoveMedia(Guid mediaId)` — if removed item was primary, promote next photo by `SortOrder`.
- `ReorderPhotos(IReadOnlyList<Guid> orderedIds)` — validates the set matches existing photo ids exactly.
- `SetPrimaryPhoto(Guid mediaId)` — only photos can be primary; clears flag on previous primary.

EF Core mapping in `PetEntityConfiguration`: `builder.OwnsMany(p => p.Media, ...)` with FK to `Pets.Id`. Unique compound index on `(PetId, IsPrimary)` filtered to `IsPrimary = 1` to enforce one primary per pet at the DB level.

New domain error codes (mapped in `ExceptionHandlingMiddleware`): `media_not_found` (404), `video_already_exists` (409), `invalid_media_order` (400), `media_not_photo` (409).

---

## API endpoints

All routes scoped under the **org-owned pet** (`/api/organizations/{orgId}/pets/{petId}/media`) for write operations — consistent with `OrgPetsController` and reuses `OrgAuthorizationFilter`. Public read uses the existing public pet routes.

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST   | `/api/organizations/{orgId}/pets/{petId}/media`               | Org Admin/Moderator | Upload photo or video (multipart/form-data) |
| GET    | `/api/pets/{petId}/media`                                     | Anonymous           | List media for a pet (ordered by SortOrder, primary first) |
| DELETE | `/api/organizations/{orgId}/pets/{petId}/media/{mediaId}`     | Org Admin/Moderator | Delete a single media item |
| PUT    | `/api/organizations/{orgId}/pets/{petId}/media/order`         | Org Admin/Moderator | Reorder photos — body: `{ orderedIds: Guid[] }` |
| PUT    | `/api/organizations/{orgId}/pets/{petId}/media/{mediaId}/primary` | Org Admin/Moderator | Mark a photo as primary |

Upload endpoint validates: content-type whitelist (`image/jpeg`, `image/png`, `image/webp`, `video/mp4`), max 10 MB photo / 50 MB video, video duration check (lightweight ffprobe-free header inspection — see Out of scope). The handler streams the file to `IMediaStorage.UploadAsync`, then calls `pet.AddPhoto` / `pet.AddVideo` with the returned URL inside a single repository transaction.

The **public** `GET /api/pets` and `GET /api/pets/{id}` query stores are extended to project a `Media` array (or at minimum a `PrimaryPhotoUrl`) into existing response DTOs so callers don't need a second round-trip for thumbnails.

---

## Storage abstraction

```csharp
public interface IMediaStorage
{
    Task<MediaUploadResult> UploadAsync(Stream content, string contentType, string fileName, CancellationToken ct);
    Task DeleteAsync(string url, CancellationToken ct);
    string GetPublicUrl(string storageKey); // for read paths that store keys instead of full URLs
}

public record MediaUploadResult(string Url, string StorageKey, long SizeBytes);
```

Two implementations registered behind config switch (`MediaStorage:Provider` = `LocalDisk` | `AzureBlob`):
- **LocalDiskMediaStorage** — writes under `wwwroot/media/{petId}/{mediaId}.{ext}` (PetService API serves `/media` as static files in Development). Used by Aspire local runs and integration tests.
- **AzureBlobMediaStorage** — uses `Azure.Storage.Blobs` against Azurite locally / Azure Blob in prod. Connection string via Aspire `builder.AddAzureStorage(...)` or fallback config.

The abstraction is the design point — additional providers (S3, GCS) drop in without domain or application changes.

---

## Frontend changes

**New component:** `Components/Shared/PetMediaGallery.razor` — carousel with thumbnail strip, full-screen lightbox, and inline `<video>` element for the video slot. Two modes via parameters: `ReadOnly` (public viewers) and `Editable` (org Admin/Moderator) which adds upload button, delete-x on each thumbnail, drag-to-reorder, and "set as primary" star.

**Updated components/pages:**
- `Pages/Discover/Discover.razor` — swipe card shows `PrimaryPhotoUrl` instead of placeholder; falls back to pet-type icon if null.
- `Pages/Pets/PetDetail.razor` — replace static placeholder image with `<PetMediaGallery PetId="@petId" ReadOnly="true" />`.
- `Pages/Organization/OrgPets.razor` — data grid adds 60×60 thumbnail column bound to `PrimaryPhotoUrl`.
- `Pages/Admin/AdminPets.razor` — same thumbnail column.
- `Components/Shared/PetFormDialog.razor` — when `IsEdit`, embed `<PetMediaGallery PetId="@PetId" Editable="true" />` below the existing fields. Create flow uploads media in a second step after the pet is created (gallery is hidden until `PetId` is known).

**API client additions** (`PetApiClient.cs`):
```csharp
Task<PetMediaResponse?> UploadPetMediaAsync(Guid orgId, Guid petId, IBrowserFile file);
Task<IReadOnlyList<PetMediaItem>> GetPetMediaAsync(Guid petId);
Task DeletePetMediaAsync(Guid orgId, Guid petId, Guid mediaId);
Task ReorderPetPhotosAsync(Guid orgId, Guid petId, IReadOnlyList<Guid> orderedIds);
Task SetPrimaryPhotoAsync(Guid orgId, Guid petId, Guid mediaId);
```

Existing `PetItem` / `OrgPetItem` records gain a `string? PrimaryPhotoUrl` field.

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `Pet` aggregate: `AddPhoto`/`AddVideo`/`RemoveMedia`/`SetPrimaryPhoto`/`ReorderPhotos` invariants — 1-video cap, primary auto-promotion on delete, reorder rejects mismatched id sets.
- Upload command handler: content-type/size validation, calls `IMediaStorage.UploadAsync` with mocked storage and verifies the returned URL is persisted.

**Integration tests** (`PetService.IntegrationTests`):
- `PetMediaControllerTests` — full upload→list→reorder→set-primary→delete flow against Testcontainers SQL + an in-memory `IMediaStorage` fake.
- Auth: anonymous gets `GET` but is rejected on writes; user from a different org is rejected on writes (`OrgAuthorizationFilter`).
- Primary-photo invariant survives concurrent `SetPrimaryPhoto` calls (DB unique filtered index).

---

## Out of scope

- **No image processing or resizing** — uploads are stored as-is. Thumbnails are rendered by browser CSS scaling. (Resizing/multiple sizes is a future spec.)
- **No CDN setup** — `IMediaStorage.GetPublicUrl` returns whatever the provider gives; CDN fronting is an infra concern.
- **No video transcoding** — only `video/mp4` accepted; duration enforced client-side before upload (HTML5 `HTMLMediaElement.duration`) with a server-side cap on file size as a defence-in-depth check. Server does not run ffmpeg/ffprobe.
- No EXIF stripping, no virus scanning, no signed/expiring URLs (public bucket).
- No bulk upload UI; one file at a time through the gallery's upload button.
