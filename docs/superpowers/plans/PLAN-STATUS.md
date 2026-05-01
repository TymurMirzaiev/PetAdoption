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
| 11 | Org Pet CRUD + Tags (Wave 2)               | `2026-05-01-org-pet-crud-with-tags.md`                |
| 12 | Adoption Process Flow (Wave 2)             | `2026-05-01-adoption-process-flow.md`                 |
| 13 | Pet Metrics per Organization (Wave 2)      | `2026-05-01-pet-metrics-per-organization.md`          |

## Remaining - Dependency Graph

```
    Benefits from Plan 10 (User Pet Filters)
    ─────────────────────────────────────────

┌───────────────────────┐
│ Discovery Algorithm   │
│ (can work without)    │
└───────────────────────┘
```

## Recommended Execution Order

### Wave 3 (benefits from User Pet Filters — Plan 10, and Plan 13's Discover tracking)
| Plan                | File                                | Scope               |
|---------------------|-------------------------------------|---------------------|
| Discovery Algorithm | `2026-05-01-discovery-algorithm.md` | Backend + frontend  |

> **Note:** Wave 1 shipped on 2026-05-01. Wave 2 (Plans 11–13) shipped on 2026-05-01 — implemented in parallel via three git worktrees, merged into `main` after build + unit + integration test verification on each branch and on the integration `dev` branch. Wave 3 is best run after Wave 2 because Discovery touches `Discover.razor` (also modified by Plan 13's impression/rejection tracking) and benefits from the existing filter infrastructure.
