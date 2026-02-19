# Service Dependencies & Integration Map

## Dependency Layers Visualization

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                LAYER 0                                       │
│                          (Independent Services)                              │
│                                                                              │
│   ┌─────────────────────┐                    ┌─────────────────────┐       │
│   │   PetService ✅     │                    │   UserService       │       │
│   │                     │                    │                     │       │
│   │ - Manage pets       │                    │ - User registration │       │
│   │ - Pet availability  │                    │ - User profiles     │       │
│   │ - Pet adoption      │                    │ - User preferences  │       │
│   │                     │                    │                     │       │
│   │ DB: PetAdoptionDb   │                    │ DB: UserDb          │       │
│   │ Events: pet.events  │                    │ Events: user.events │       │
│   └─────────────────────┘                    └─────────────────────┘       │
│                                                                              │
└──────────────────┬───────────────────────────────────┬───────────────────────┘
                   │                                   │
                   │ HTTP + Events                     │ HTTP + Events
                   │                                   │
┌──────────────────┴───────────────────────────────────┴───────────────────────┐
│                                LAYER 1                                       │
│                        (Foundation Business Logic)                           │
│                                                                              │
│                       ┌─────────────────────────┐                           │
│                       │  ChallengeService       │                           │
│                       │                         │                           │
│                       │ - Create challenges     │                           │
│                       │ - Manage participants   │                           │
│                       │ - Registration          │                           │
│                       │                         │                           │
│                       │ Depends on:             │                           │
│                       │  └─ UserService (HTTP)  │                           │
│                       │                         │                           │
│                       │ DB: ChallengeDb         │                           │
│                       │ Events: challenge.events│                           │
│                       └─────────────────────────┘                           │
│                                                                              │
└────────────────────────────────┬─────────────────────────────────────────────┘
                                 │
                                 │ Events: challenge.created.v1
                                 │         challenge.cancelled.v1
                                 │
┌────────────────────────────────┴─────────────────────────────────────────────┐
│                                LAYER 2                                       │
│                           (Time-Based Triggers)                              │
│                                                                              │
│                       ┌─────────────────────────┐                           │
│                       │  SchedulerService       │                           │
│                       │     (Background)        │                           │
│                       │                         │                           │
│                       │ - Schedule distribution │                           │
│                       │ - Trigger at time       │                           │
│                       │ - Job persistence       │                           │
│                       │                         │                           │
│                       │ Depends on:             │                           │
│                       │  └─ ChallengeService    │                           │
│                       │     (via events)        │                           │
│                       │                         │                           │
│                       │ DB: HangfireDb (PG)     │                           │
│                       │ Publishes:              │                           │
│                       │  challenge.ready-for-   │                           │
│                       │  distribution.v1        │                           │
│                       └─────────────────────────┘                           │
│                                                                              │
└────────────────────────────────┬─────────────────────────────────────────────┘
                                 │
                                 │ Events: challenge.ready-for-distribution.v1
                                 │
┌────────────────────────────────┴─────────────────────────────────────────────┐
│                                LAYER 3                                       │
│                          (Matching Algorithm)                                │
│                                                                              │
│                       ┌─────────────────────────┐                           │
│                       │ DistributionService     │                           │
│                       │                         │                           │
│                       │ - Match pets to users   │                           │
│                       │ - Matching algorithm    │                           │
│                       │ - Distribution logic    │                           │
│                       │                         │                           │
│                       │ Depends on:             │                           │
│                       │  ├─ SchedulerService    │                           │
│                       │  │  (via events)        │                           │
│                       │  ├─ ChallengeService    │                           │
│                       │  │  (HTTP - get         │                           │
│                       │  │   participants)      │                           │
│                       │  └─ PetService          │                           │
│                       │     (HTTP - get pets)   │                           │
│                       │                         │                           │
│                       │ DB: DistributionDb      │                           │
│                       │ Events: distribution.*  │                           │
│                       └─────────────────────────┘                           │
│                                                                              │
└────────────────────────────────┬─────────────────────────────────────────────┘
                                 │
                                 │ Events: distribution.match-created.v1
                                 │
┌────────────────────────────────┴─────────────────────────────────────────────┐
│                                LAYER 4                                       │
│                         (Adoption Workflow)                                  │
│                                                                              │
│                       ┌─────────────────────────┐                           │
│                       │   AdoptionService       │                           │
│                       │                         │                           │
│                       │ - Adoption applications │                           │
│                       │ - Approval workflow     │                           │
│                       │ - Application status    │                           │
│                       │                         │                           │
│                       │ Depends on:             │                           │
│                       │  ├─ DistributionService │                           │
│                       │  │  (via events)        │                           │
│                       │  └─ PetService          │                           │
│                       │     (HTTP - update      │                           │
│                       │      pet status)        │                           │
│                       │                         │                           │
│                       │ DB: AdoptionDb          │                           │
│                       │ Events: adoption.*      │                           │
│                       └─────────────────────────┘                           │
│                                                                              │
└────────────────────────────────┬─────────────────────────────────────────────┘
                                 │
                                 │ Events: All events from all services
                                 │
┌────────────────────────────────┴─────────────────────────────────────────────┐
│                                LAYER 5                                       │
│                        (Cross-Cutting Concerns)                              │
│                                                                              │
│                       ┌─────────────────────────┐                           │
│                       │ NotificationService     │                           │
│                       │     (Background)        │                           │
│                       │                         │                           │
│                       │ - Email notifications   │                           │
│                       │ - SMS (future)          │                           │
│                       │ - Push notifications    │                           │
│                       │                         │                           │
│                       │ Depends on:             │                           │
│                       │  └─ ALL SERVICES        │                           │
│                       │     (consumes all       │                           │
│                       │      events)            │                           │
│                       │                         │                           │
│                       │ DB: NotificationDb      │                           │
│                       │ No events published     │                           │
│                       └─────────────────────────┘                           │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Communication Patterns

### Synchronous (HTTP)
Used for **queries** and **immediate validation**

```
DistributionService ──HTTP GET──> ChallengeService
                                   (Get participants list)

DistributionService ──HTTP GET──> PetService
                                   (Get available pets)

AdoptionService ──HTTP PUT──> PetService
                              (Update pet status: reserve/adopt)

ChallengeService ──HTTP GET──> UserService
                               (Validate user exists)
```

### Asynchronous (Events via RabbitMQ)
Used for **state changes** and **cross-service notifications**

```
ChallengeService
  │
  ├─ challenge.created.v1 ──────────> SchedulerService
  ├─ challenge.cancelled.v1 ─────────> SchedulerService
  ├─ challenge.participant-registered.v1 ──> NotificationService
  └─ challenge.ready-for-distribution.v1
      └─────────────────────────────> DistributionService

DistributionService
  │
  ├─ distribution.match-created.v1 ──> AdoptionService
  │                                 └──> NotificationService
  └─ distribution.completed.v1 ─────> NotificationService

AdoptionService
  │
  ├─ adoption.application-created.v1 ──> NotificationService
  ├─ adoption.application-approved.v1 ──> NotificationService
  ├─ adoption.application-rejected.v1 ──> NotificationService
  └─ adoption.application-completed.v1 ──> NotificationService

UserService
  │
  └─ user.registered.v1 ──────────────> NotificationService
```

---

## Data Flow Example: Complete Challenge Flow

### Step 1: Challenge Creation
```
Admin (Web UI)
  │
  └─> POST /api/challenges
        │
        └─> ChallengeService
              ├─> Save to ChallengeDb
              └─> Publish: challenge.created.v1 ──┐
                                                   │
                                                   └─> SchedulerService
                                                         └─> Schedule job for
                                                             distributionDateTime
```

### Step 2: User Registration
```
User (Web UI)
  │
  └─> POST /api/challenges/{id}/register
        │
        └─> ChallengeService
              ├─> HTTP GET /api/users/{id} ──> UserService (validate)
              ├─> Save participant to ChallengeDb
              └─> Publish: challenge.participant-registered.v1
                    │
                    └─> NotificationService
                          └─> Send confirmation email
```

### Step 3: Scheduled Distribution (Automatic)
```
Scheduler (Time-based)
  │
  └─> At distributionDateTime
        │
        └─> SchedulerService
              └─> Publish: challenge.ready-for-distribution.v1
                    │
                    └─> DistributionService
                          ├─> HTTP GET /api/challenges/{id}/participants
                          │     └─> ChallengeService (get list)
                          │
                          ├─> HTTP GET /api/pets?status=available
                          │     └─> PetService (get list)
                          │
                          ├─> Run matching algorithm
                          │     └─> Create matches (random or preference-based)
                          │
                          ├─> Save to DistributionDb
                          │
                          └─> For each match:
                                └─> Publish: distribution.match-created.v1
                                      ├─> AdoptionService
                                      │     ├─> Create adoption application
                                      │     └─> Publish: adoption.application-created.v1
                                      │
                                      └─> NotificationService
                                            └─> Send "You got matched!" email
```

### Step 4: Adoption Approval
```
Admin (Web UI)
  │
  └─> PUT /api/adoptions/applications/{id}/approve
        │
        └─> AdoptionService
              ├─> Update status to "Approved"
              ├─> HTTP PUT /api/pets/{id}/adopt
              │     └─> PetService (mark as adopted)
              │
              └─> Publish: adoption.application-approved.v1
                    │
                    └─> NotificationService
                          └─> Send approval email
```

### Step 5: Adoption Completion
```
User (Web UI)
  │
  └─> PUT /api/adoptions/applications/{id}/complete
        │
        └─> AdoptionService
              ├─> Update status to "Completed"
              └─> Publish: adoption.application-completed.v1
                    │
                    └─> NotificationService
                          └─> Send congratulations email
```

---

## Service-to-Service Dependencies Matrix

| Service | Depends On (HTTP) | Depends On (Events) | Provides (HTTP) | Publishes (Events) |
|---------|-------------------|---------------------|-----------------|-------------------|
| **PetService** | - | - | Pet CRUD, Availability | pet.* |
| **UserService** | - | - | User CRUD, Validation | user.* |
| **ChallengeService** | UserService | - | Challenge CRUD, Participants | challenge.* |
| **SchedulerService** | - | ChallengeService | - | challenge.ready-for-distribution.v1 |
| **DistributionService** | ChallengeService, PetService | SchedulerService | Distribution queries | distribution.* |
| **AdoptionService** | PetService | DistributionService | Adoption CRUD | adoption.* |
| **NotificationService** | - | ALL services | - | - |

---

## Database Dependencies

### Per Service

| Service | Database | Type | Collections/Tables |
|---------|----------|------|-------------------|
| PetService | PetAdoptionDb | MongoDB | Pets, OutboxEvents |
| UserService | UserDb | MongoDB | Users, OutboxEvents |
| ChallengeService | ChallengeDb | MongoDB | Challenges, Participants, OutboxEvents |
| SchedulerService | HangfireDb | PostgreSQL | Hangfire tables (jobs, queues, etc.) |
| DistributionService | DistributionDb | MongoDB | Distributions, Matches, OutboxEvents |
| AdoptionService | AdoptionDb | MongoDB | AdoptionApplications, OutboxEvents |
| NotificationService | NotificationDb | MongoDB | Notifications, NotificationTemplates |

### Shared Infrastructure
- **MongoDB:** All services except SchedulerService
- **PostgreSQL:** Only SchedulerService (for Hangfire)
- **RabbitMQ:** All services

---

## RabbitMQ Topology

### Exchanges
```
pet.events (topic)
  ├─ pet.reserved.v1
  ├─ pet.adopted.v1
  ├─ pet.reservation.cancelled.v1
  ├─ pet.reserved-for-challenge.v1 (future)
  └─ pet.released-from-challenge.v1 (future)

user.events (topic)
  ├─ user.registered.v1
  ├─ user.profile.updated.v1
  └─ user.suspended.v1

challenge.events (topic)
  ├─ challenge.created.v1
  ├─ challenge.opened.v1
  ├─ challenge.participant-registered.v1
  ├─ challenge.participant-unregistered.v1
  ├─ challenge.ready-for-distribution.v1
  ├─ challenge.completed.v1
  └─ challenge.cancelled.v1

distribution.events (topic)
  ├─ distribution.started.v1
  ├─ distribution.match-created.v1
  ├─ distribution.completed.v1
  └─ distribution.failed.v1

adoption.events (topic)
  ├─ adoption.application-created.v1
  ├─ adoption.application-approved.v1
  ├─ adoption.application-rejected.v1
  ├─ adoption.application-completed.v1
  └─ adoption.application-cancelled.v1
```

### Queue Bindings

#### SchedulerService Queues
```
scheduler.challenge-created-handler
  └─ Binding: challenge.events / challenge.created.v1

scheduler.challenge-cancelled-handler
  └─ Binding: challenge.events / challenge.cancelled.v1
```

#### DistributionService Queues
```
distribution.challenge-ready-handler
  └─ Binding: challenge.events / challenge.ready-for-distribution.v1
```

#### AdoptionService Queues
```
adoption.match-created-handler
  └─ Binding: distribution.events / distribution.match-created.v1
```

#### NotificationService Queues
```
notification.challenge-events
  └─ Binding: challenge.events / challenge.#

notification.distribution-events
  └─ Binding: distribution.events / distribution.#

notification.adoption-events
  └─ Binding: adoption.events / adoption.#

notification.user-events
  └─ Binding: user.events / user.#
```

---

## Critical Dependencies Summary

### To Start Development

**Phase 1 - Can Start Immediately:**
- UserService (no dependencies)
- PetService ✅ (already done)

**Phase 1 - After UserService:**
- ChallengeService (needs UserService for validation)

**Phase 2 - After ChallengeService:**
- SchedulerService (needs ChallengeService events + PostgreSQL)

**Phase 3 - After SchedulerService:**
- DistributionService (needs SchedulerService events, ChallengeService API, PetService API)

**Phase 4 - After DistributionService:**
- AdoptionService (needs DistributionService events, PetService API)

**Phase 5 - After All Services:**
- NotificationService (needs all services' events)

### Infrastructure Prerequisites

**Before UserService:**
- MongoDB (already available ✅)
- RabbitMQ (already available ✅)

**Before SchedulerService:**
- PostgreSQL (need to add to compose.yaml)

**Before NotificationService:**
- Email provider (SendGrid API key or SMTP server)

---

## Ports Allocation

| Service | HTTP Port | Management Port | Notes |
|---------|-----------|-----------------|-------|
| PetService | 8080 | - | ✅ Already configured |
| UserService | 8081 | - | To be configured |
| ChallengeService | 8082 | - | To be configured |
| DistributionService | 8083 | - | To be configured |
| AdoptionService | 8084 | - | To be configured |
| SchedulerService | - | 5000 (Hangfire) | Background worker |
| NotificationService | - | - | Background worker |
| MongoDB | 27017 | - | ✅ Already configured |
| RabbitMQ | 5672 | 15672 | ✅ Already configured |
| PostgreSQL | 5432 | - | To be configured |
| Web UI | 8090 | - | Optional |

---

## Testing Dependencies

### Unit Tests (No External Dependencies)
Each service can be unit tested independently with mocked dependencies.

### Integration Tests Dependencies

| Service | Requires Running |
|---------|------------------|
| UserService | MongoDB, RabbitMQ |
| ChallengeService | MongoDB, RabbitMQ, UserService API (or mock) |
| SchedulerService | PostgreSQL, RabbitMQ |
| DistributionService | MongoDB, RabbitMQ, ChallengeService API (mock), PetService API (mock) |
| AdoptionService | MongoDB, RabbitMQ, PetService API (mock) |
| NotificationService | MongoDB, RabbitMQ, Email mock |

**Recommendation:** Use Testcontainers for MongoDB, PostgreSQL, and RabbitMQ in integration tests.

---

## Deployment Order

### Development Environment
1. Infrastructure: MongoDB, RabbitMQ
2. PetService ✅
3. UserService
4. Infrastructure: PostgreSQL
5. ChallengeService
6. SchedulerService
7. DistributionService
8. AdoptionService
9. NotificationService

### Production Environment
**Same order, but with additional steps:**
- Run database migrations before deploying each service
- Verify health checks after each deployment
- Monitor RabbitMQ queue depth
- Use blue-green or canary deployments for safety

---

**Last Updated:** 2026-02-20
**Document Version:** 1.0
**Status:** Reference Documentation
