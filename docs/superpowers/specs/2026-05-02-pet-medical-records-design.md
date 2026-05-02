# Pet Medical Records — Design Spec

**Date:** 2026-05-02
**Status:** Draft
**Scope:** PetService backend (domain extension on `Pet` + new endpoints on `OrgPetsController`) + Blazor WASM frontend (new tab in `PetFormDialog`, new section on pet detail page)

---

## Overview

Adopters need to know a pet's medical history before committing — vaccination status, spay/neuter, allergies, microchip ID, and recent vet activity directly affect adoption decisions and the cost the adopter inherits. Today the `Pet` aggregate carries none of this information. We add a `PetMedicalRecord` owned entity to `Pet`, expose it publicly on the pet detail page (read), and let org Admin/Moderator users edit it via the existing `OrgPetsController` (write).

---

## Architecture

Clean Architecture, write path through the existing custom mediator. **Recommendation: extend `OrgPetsController` rather than introduce a `MedicalRecordsController`.** The medical record is owned data on `Pet` (1:1, lifecycle-bound), org auth is already in place via `OrgAuthorizationFilter`, and the existing `PUT /api/organizations/{orgId}/pets/{petId}` already does partial pet updates — adding a sibling `PUT .../pets/{petId}/medical-record` keeps everything under the same route, filter, and mental model. A separate controller would duplicate the org-scoping plumbing for no domain gain.

Read path: medical record is returned inline on the existing public `GET /api/pets/{id}` (it is part of the Pet aggregate). No new query store, no new query handler.

---

## Domain changes

### `PetMedicalRecord` — owned entity on `Pet`

Owned 1:1 entity (EF Core `OwnsOne`). Created lazily on first update; `Pet.MedicalRecord` is nullable until populated. Stored in the `Pets` table as flattened columns plus two child tables for the collections.

```csharp
public class PetMedicalRecord
{
    public bool IsSpayedNeutered { get; private set; }
    public DateOnly? SpayNeuterDate { get; private set; }
    public MicrochipId? MicrochipId { get; private set; }
    public MedicalNotes? History { get; private set; }       // <= 5000 chars
    public DateOnly? LastVetVisit { get; private set; }
    public IReadOnlyList<Vaccination> Vaccinations { get; }   // OwnsMany
    public IReadOnlyList<Allergy> Allergies { get; }          // OwnsMany of value object
    public DateTime UpdatedAt { get; private set; }
}
```

### Value objects

| Type | Rules |
|------|-------|
| `MicrochipId` | 8–23 alphanumeric chars (covers ISO 11784/11785 15-digit + legacy 9–10-digit formats); trimmed; uppercased |
| `MedicalNotes` | 1–5000 chars, trimmed |
| `Allergy` | 1–100 chars, trimmed |
| `Vaccination` | `VaccineType` (1–100 chars), `AdministeredOn` (DateOnly, required), `NextDueOn` (DateOnly, optional, must be ≥ AdministeredOn), `Notes` (optional, ≤ 500 chars) |

All value objects validate in their constructor and throw `DomainException` with a new error code group (`invalid_microchip_id`, `invalid_medical_notes`, `invalid_allergy`, `invalid_vaccination`). The exception middleware maps these to 400.

### `Pet.UpdateMedicalRecord(...)`

Single method on the aggregate that fully replaces the record (no per-field setters — keeps the aggregate boundary clean and the API simpler):

```csharp
public void UpdateMedicalRecord(
    bool isSpayedNeutered, DateOnly? spayNeuterDate,
    string? microchipId, string? historyNotes,
    DateOnly? lastVetVisit,
    IEnumerable<VaccinationInput> vaccinations,
    IEnumerable<string> allergies);
```

Raises `PetMedicalRecordUpdatedEvent(PetId, UpdatedAt)` on success. Published via the existing transactional outbox to the `pet.events` exchange — downstream consumers (e.g. notification service) can subscribe later.

### EF Core mapping

In `PetEntityConfiguration`:

- `builder.OwnsOne(p => p.MedicalRecord, mr => { ... })` — flattens scalars into the `Pets` table (`MedicalRecord_IsSpayedNeutered`, etc.)
- Inside `OwnsOne`: `mr.OwnsMany(x => x.Vaccinations, ...)` → table `PetVaccinations`
- Inside `OwnsOne`: `mr.OwnsMany(x => x.Allergies, ...)` → table `PetAllergies`
- `MicrochipId`, `MedicalNotes`, `Allergy` mapped via `HasConversion` (string ↔ value object)
- No new repository — `IPetRepository` already loads the full aggregate

---

## API endpoints

### Read (public, existing)

`GET /api/pets/{id}` — response DTO gains an optional `MedicalRecord` property. Anonymous; no auth changes.

### Write (org-scoped, new)

`PUT /api/organizations/{orgId}/pets/{petId}/medical-record` — Admin/Moderator only via `OrgAuthorizationFilter`. Body is `UpdatePetMedicalRecordRequest` (mirrors the domain method signature). Returns `200 OK` with the updated record. Sends `UpdatePetMedicalRecordCommand` through the mediator.

`UpdateOrgPetRequest` is **not** extended — keeping the medical record on its own endpoint avoids forcing the basic edit form to round-trip vaccination arrays it doesn't touch, and keeps the optimistic-concurrency surface narrower.

---

## Frontend

### `PetFormDialog.razor` — new "Medical" tab

Wrap the existing form fields in a `MudTabs` control: tab 1 "Details" (existing), tab 2 "Medical" (new). The Medical tab is only rendered when editing an existing pet (medical record needs a `PetId` to PUT against — for new pets the user saves Details first, then the Medical tab becomes available). MudBlazor controls:

- `MudSwitch` — Spayed/Neutered + `MudDatePicker` for date (shown when switch is on)
- `MudTextField` — Microchip ID (with inline regex validation)
- `MudDatePicker` — Last vet visit
- `MudTextField Lines="6"` with 5000-char counter — Medical history notes
- `MudChipSet` editable — Allergies (free-text chip add/remove)
- Vaccinations: `MudTable` with inline add/edit row (`Type` text + `Date` picker + `NextDue` picker + `Notes` text + delete button)
- Save button calls new `PetApiClient.UpdatePetMedicalRecordAsync(orgId, petId, request)`

Visible only to org Admin/Moderator (existing role guard on the dialog already enforces this).

### `PetDetailsPage.razor` — new read-only "Medical" section

A `MudCard` between Description and the (existing) Tags section. Public — anonymous adopters see it. Layout:

- Two-column `MudGrid`: left column shows Spay/Neuter (chip green/grey), Microchip (mono font), Last vet visit (relative date)
- Allergies: `MudChip` list (warning color); empty → "No known allergies"
- Vaccinations: small `MudTable` (Type · Date · Next due) sorted by date desc; "Next due" cell highlighted red when overdue (`NextDueOn < today`)
- Medical history: collapsible `MudExpansionPanel` ("Show notes")

Section is hidden entirely if `MedicalRecord == null`.

### API client + models

`PetApiClient.UpdatePetMedicalRecordAsync(Guid orgId, Guid petId, UpdatePetMedicalRecordRequest request)`. New client-side records in `Models/ApiModels.cs` mirroring backend DTOs (`MedicalRecordModel`, `VaccinationModel`, `UpdatePetMedicalRecordRequest`). Existing `PetDetailsResponse` gains an optional `MedicalRecord` property.

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `MicrochipIdTests`, `MedicalNotesTests`, `VaccinationTests`, `AllergyTests` — value object validation (empty/whitespace/length/format) using `[Theory]` + `// Act & Assert`
- `PetMedicalRecordTests` — `Pet.UpdateMedicalRecord` creates record on first call, replaces on second, raises `PetMedicalRecordUpdatedEvent`, refuses negative `NextDueOn < AdministeredOn`
- `UpdatePetMedicalRecordCommandHandlerTests` — mocks `IPetRepository`, asserts persistence + event publish, asserts org mismatch throws

**Integration tests** (`PetService.IntegrationTests`):
- `OrgPetsControllerMedicalRecordTests` — Admin/Moderator can PUT, regular user gets 403, cross-org user gets 403, GET `/api/pets/{id}` returns the record anonymously, invalid microchip returns 400

---

## Out of scope

- Vet portal integration (no third-party vet system sync)
- Prescription / medication tracking
- File attachments (scanned vet records, X-rays, lab results)
- Vaccination reminder emails / push notifications (event is published; consumer is a future feature)
- Audit trail of who edited what (covered separately if/when a general audit log feature lands)
