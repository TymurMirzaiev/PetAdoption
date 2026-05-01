# Implementation Plans - Status & Dependencies

## Completed

| #  | Plan                                       | File                                                  |
|----|--------------------------------------------|-------------------------------------------------------|
| 1  | Integration Tests with Fluent Builders     | `2026-04-27-integration-tests.md`                     |
| 2  | PetService CRUD Endpoints                  | `2026-04-28-petservice-crud-endpoints.md`             |
| 3  | PetService Backend Enhancements            | `2026-04-29-petservice-backend-enhancements.md`       |
| 4  | UserService Backend Enhancements           | `2026-04-29-userservice-backend-enhancements.md`      |
| 5  | Blazor UI                                  | `2026-04-29-blazor-ui.md`                             |
| 6  | Organization Management + Platform Admin   | `2026-05-01-organization-management-platform-admin.md`|
| 7  | Seed Data                                  | `2026-05-01-seed-data.md`                             |
| 8  | Onboarding Intro (Wave 1)                  | `2026-05-01-onboarding-intro.md`                      |
| 9  | Favorites Management (Wave 1)              | `2026-05-01-favorites-management.md`                  |
| 10 | User Pet Filters (Wave 1)                  | `2026-05-01-user-pet-filters.md`                      |

## Remaining - Dependency Graph

```
    Depends on Plan 6 (Org Mgmt)        Benefits from Plan 10 (User Pet Filters)
    ────────────────────────────        ─────────────────────────────────────────

┌───────────────────────┐               ┌───────────────────────┐
│ Org Pet CRUD + Tags   │               │ Discovery Algorithm   │
│                       │               │ (can work without)    │
└───────────────────────┘               └───────────────────────┘

┌───────────────────────┐
│ Adoption Process Flow │
│                       │
└───────────────────────┘

┌───────────────────────┐
│ Pet Metrics per Org   │
│                       │
└───────────────────────┘
```

## Recommended Execution Order

### Wave 2 (parallel - depends on Plan 6, already completed)
| Plan                  | File                                       | Scope                         |
|-----------------------|--------------------------------------------|-------------------------------|
| Org Pet CRUD + Tags   | `2026-05-01-org-pet-crud-with-tags.md`     | Backend (PetService)          |
| Adoption Process Flow | `2026-05-01-adoption-process-flow.md`      | Backend (new aggregate)       |
| Pet Metrics per Org   | `2026-05-01-pet-metrics-per-organization.md`| Backend + frontend           |

### Wave 3 (benefits from User Pet Filters — Plan 10)
| Plan                | File                                | Scope               |
|---------------------|-------------------------------------|---------------------|
| Discovery Algorithm | `2026-05-01-discovery-algorithm.md` | Backend + frontend  |

> **Note:** Wave 1 (Onboarding, Favorites Management, User Pet Filters) shipped on 2026-05-01 — all three plans merged into `main` with full integration test coverage on a real SQL Server (Testcontainers). Wave 2 can begin immediately; Wave 3 is best run after Wave 2 since Discovery benefits from filters being live.
