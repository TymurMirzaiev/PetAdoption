# Implementation Plans - Status & Dependencies

## Completed

| # | Plan | File |
|---|------|------|
| 1 | Integration Tests with Fluent Builders | `2026-04-27-integration-tests.md` |
| 2 | PetService CRUD Endpoints | `2026-04-28-petservice-crud-endpoints.md` |
| 3 | PetService Backend Enhancements | `2026-04-29-petservice-backend-enhancements.md` |
| 4 | UserService Backend Enhancements | `2026-04-29-userservice-backend-enhancements.md` |
| 5 | Blazor UI | `2026-04-29-blazor-ui.md` |
| 6 | Organization Management + Platform Admin | `2026-05-01-organization-management-platform-admin.md` |
| 7 | Seed Data | `2026-05-01-seed-data.md` |

## Remaining - Dependency Graph

```
    Independent                 Depends on Plan 6 (Org Mgmt)
    ──────────                  ────────────────────────────

┌──────────────────┐        ┌───────────────────────┐
│ Favorites Mgmt   │        │ Org Pet CRUD + Tags   │
│ (enhancements)   │        │                       │
└──────────────────┘        └───────────────────────┘

┌──────────────────┐        ┌───────────────────────┐
│ Onboarding Intro │        │ Adoption Process Flow │
│ (frontend only)  │        │                       │
└──────────────────┘        └───────────────────────┘

┌──────────────────┐        ┌───────────────────────┐
│ User Pet Filters │──opt──>│ Discovery Algorithm   │
│                  │        │ (can work without)    │
└──────────────────┘        └───────────────────────┘

                            ┌───────────────────────┐
                            │ Pet Metrics per Org   │
                            │                       │
                            └───────────────────────┘
```

## Recommended Execution Order

### Wave 1 (parallel - no dependencies)
| Plan | File | Scope |
|------|------|-------|
| Favorites Management | `2026-05-01-favorites-management.md` | Backend + frontend enhancements |
| Onboarding Intro | `2026-05-01-onboarding-intro.md` | Frontend only |
| User Pet Filters | `2026-05-01-user-pet-filters.md` | Backend + frontend |

### Wave 2 (parallel - depends on Plan 6, already completed)
| Plan | File | Scope |
|------|------|-------|
| Org Pet CRUD + Tags | `2026-05-01-org-pet-crud-with-tags.md` | Backend (PetService) |
| Adoption Process Flow | `2026-05-01-adoption-process-flow.md` | Backend (new aggregate) |
| Pet Metrics per Org | `2026-05-01-pet-metrics-per-organization.md` | Backend + frontend |

### Wave 3 (benefits from User Pet Filters)
| Plan | File | Scope |
|------|------|-------|
| Discovery Algorithm | `2026-05-01-discovery-algorithm.md` | Backend + frontend |

> **Note:** Wave 1 and Wave 2 can run in parallel since Wave 2's dependency (Organization Management) is already completed. The waves are grouped by logical cohesion, not hard sequencing. The only real sequencing constraint is Discovery Algorithm benefiting from User Pet Filters being done first.
