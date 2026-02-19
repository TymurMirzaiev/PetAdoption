# PetAdoption Project - Implementation Checklist

Quick reference guide for tracking implementation progress.

## Service Dependency Tree

```
LAYER 0 (Independent - Can start immediately)
  ├── PetService ✅ (DONE)
  └── UserService ⬜ START HERE

LAYER 1 (Requires Layer 0)
  └── ChallengeService ⬜
      └── Depends on: UserService

LAYER 2 (Requires Layer 0-1)
  └── SchedulerService ⬜
      └── Depends on: ChallengeService

LAYER 3 (Requires Layer 0-2)
  └── DistributionService ⬜
      └── Depends on: ChallengeService, PetService, SchedulerService (events)

LAYER 4 (Requires Layer 0-3)
  └── AdoptionService ⬜
      └── Depends on: DistributionService, PetService

LAYER 5 (Requires all layers)
  └── NotificationService ⬜
      └── Depends on: ALL services (consumes all events)
```

## Critical Path
**Must be implemented in this order:**

1. UserService → 2. ChallengeService → 3. SchedulerService → 4. DistributionService → 5. AdoptionService → 6. NotificationService

---

## Phase 1: Foundation Services

### UserService ⬜
**Status:** Not Started | **Priority:** P0 - Critical | **Effort:** 5-8 days

#### Prerequisites
- [ ] None (can start immediately)

#### Core Tasks
- [ ] Project setup (API, Application, Domain, Infrastructure)
- [ ] Domain: `User` aggregate, `Email`, `FullName`, `PhoneNumber` value objects
- [ ] Events: `UserRegisteredEvent`, `UserProfileUpdatedEvent`
- [ ] Application: Register, Update, GetById, GetByEmail commands/queries
- [ ] Infrastructure: MongoDB repositories, RabbitMQ publisher
- [ ] API: Users controller with 5 endpoints
- [ ] Tests: Unit tests for domain, integration tests for API
- [ ] Docker: Dockerfile, add to compose.yaml (port 8081)

#### Definition of Done
- [ ] User can register with email and name
- [ ] User can update profile
- [ ] User can be queried by ID or email
- [ ] Events published to `user.events` exchange
- [ ] API running in Docker on port 8081
- [ ] Tests passing with 80%+ coverage

#### Blocked By
None - **Can start immediately**

---

### ChallengeService ⬜
**Status:** Not Started | **Priority:** P0 - Critical | **Effort:** 8-10 days

#### Prerequisites
- [ ] UserService completed and running

#### Core Tasks
- [ ] Project setup (4 layers)
- [ ] Domain: `AdoptionChallenge` and `Participant` aggregates
- [ ] Domain: `ChallengeStatus` enum, `UserPreferences` value object
- [ ] Events: 7 events (created, opened, participant registered/unregistered, ready-for-distribution, completed, cancelled)
- [ ] Application: 6 commands (create, open, register, unregister, complete, cancel)
- [ ] Application: 4 queries (by ID, active, participants, user challenges)
- [ ] Infrastructure: MongoDB (2 collections), RabbitMQ, HTTP client for UserService
- [ ] API: Challenges controller with 8 endpoints
- [ ] Authorization: Admin vs regular user
- [ ] Tests: Domain, handlers, API, event publishing
- [ ] Docker: Dockerfile, add to compose.yaml (port 8082)

#### Business Rules
- [ ] Max participants validation
- [ ] Registration deadline check
- [ ] Duplicate registration prevention
- [ ] Challenge status state machine

#### Definition of Done
- [ ] Admin can create and open challenges
- [ ] Users can register/unregister for challenges
- [ ] Participants list can be queried
- [ ] Events published to `challenge.events` exchange
- [ ] Integration with UserService for user validation
- [ ] API running in Docker on port 8082
- [ ] Tests passing with 80%+ coverage

#### Blocked By
- [ ] UserService (need user validation)

---

## Phase 2: Scheduling Infrastructure

### SchedulerService ⬜
**Status:** Not Started | **Priority:** P0 - Critical | **Effort:** 5-7 days

#### Prerequisites
- [ ] ChallengeService completed and running
- [ ] PostgreSQL added to infrastructure

#### Core Tasks
- [ ] Create Background Worker project (not Web API)
- [ ] Add Hangfire with PostgreSQL persistence
- [ ] Domain: `ScheduledJob` entity
- [ ] Application: Job scheduler service, event handlers
- [ ] Infrastructure: RabbitMQ consumer (challenge events), RabbitMQ publisher (trigger events)
- [ ] Jobs: `TriggerDistributionJob` - publishes `challenge.ready-for-distribution.v1`
- [ ] Event handlers: ChallengeCreated → schedule job, ChallengeCancelled → cancel job
- [ ] Tests: Job scheduling, cancellation, missed job recovery
- [ ] Docker: PostgreSQL for Hangfire, Dockerfile for worker, add to compose.yaml

#### Configuration
- [ ] PostgreSQL database for Hangfire
- [ ] RabbitMQ queue: `scheduler.challenge-created-handler`
- [ ] RabbitMQ queue: `scheduler.challenge-cancelled-handler`
- [ ] Hangfire dashboard (optional for dev)

#### Definition of Done
- [ ] Service starts and connects to RabbitMQ
- [ ] Listens to `challenge.created.v1` and schedules distribution job
- [ ] Triggers `challenge.ready-for-distribution.v1` at scheduled time
- [ ] Listens to `challenge.cancelled.v1` and cancels scheduled job
- [ ] Jobs persist and survive service restarts
- [ ] Running in Docker with health checks
- [ ] Tests passing for job lifecycle

#### Blocked By
- [ ] ChallengeService (need challenge.created.v1 events)
- [ ] PostgreSQL infrastructure

---

## Phase 3: Core Feature Flow

### DistributionService ⬜
**Status:** Not Started | **Priority:** P0 - Critical | **Effort:** 10-12 days

#### Prerequisites
- [ ] ChallengeService running
- [ ] PetService running ✅
- [ ] SchedulerService running and triggering events

#### Core Tasks
- [ ] Project setup (Web API + 4 layers)
- [ ] Domain: `Distribution` aggregate, `Match` entity
- [ ] Domain: `IMatchingStrategy` interface, `RandomMatchingStrategy` implementation
- [ ] Events: `DistributionStartedEvent`, `MatchCreatedEvent`, `DistributionCompletedEvent`, `DistributionFailedEvent`
- [ ] Application: Start/Retry distribution commands, queries for distributions and matches
- [ ] Application: HTTP clients for ChallengeService and PetService
- [ ] Application: Matching algorithm (Fisher-Yates shuffle)
- [ ] Infrastructure: MongoDB (2 collections), RabbitMQ consumer/publisher
- [ ] Infrastructure: Event handler for `challenge.ready-for-distribution.v1`
- [ ] API: Distributions controller (mostly admin/monitoring)
- [ ] Error handling: Insufficient pets, service unavailability, idempotency
- [ ] Tests: Matching algorithm, domain logic, event processing, end-to-end
- [ ] Docker: Dockerfile, add to compose.yaml (port 8083)

#### Matching Algorithm
- [ ] Random shuffle (Fisher-Yates)
- [ ] Handle edge cases: more participants than pets, vice versa
- [ ] Calculate basic match score
- [ ] Log matching statistics

#### Definition of Done
- [ ] Service consumes `challenge.ready-for-distribution.v1`
- [ ] Fetches participants from ChallengeService API
- [ ] Fetches available pets from PetService API
- [ ] Runs matching algorithm successfully
- [ ] Publishes `distribution.match-created.v1` for each match
- [ ] Publishes `distribution.completed.v1` when done
- [ ] Handles failures gracefully (insufficient pets, service down)
- [ ] Idempotent event processing (no duplicate matches)
- [ ] API running in Docker on port 8083
- [ ] Tests passing including end-to-end challenge → distribution flow

#### Blocked By
- [ ] SchedulerService (need ready-for-distribution event)
- [ ] ChallengeService (need participants API)
- [ ] PetService (need available pets API) - may need enhancement

---

### AdoptionService ⬜
**Status:** Not Started | **Priority:** P0 - Critical | **Effort:** 8-10 days

#### Prerequisites
- [ ] DistributionService running and publishing match events
- [ ] PetService API accessible

#### Core Tasks
- [ ] Project setup (Web API + 4 layers)
- [ ] Domain: `AdoptionApplication` aggregate
- [ ] Domain: `ApplicationStatus` enum, `ApplicationSource` enum
- [ ] Domain: State machine (Initiated → UnderReview → Approved/Rejected → Completed/Cancelled)
- [ ] Events: Created, Approved, Rejected, Completed, Cancelled
- [ ] Application: 6 commands (create from match, create direct, approve, reject, complete, cancel)
- [ ] Application: 5 queries (by ID, by user, by pet, by status, pending queue)
- [ ] Infrastructure: MongoDB, RabbitMQ, HTTP client for PetService
- [ ] Infrastructure: Event handler for `distribution.match-created.v1`
- [ ] API: Adoptions controller with 7 endpoints
- [ ] Authorization: User vs admin permissions
- [ ] Business rules: Active applications limit, duplicate prevention
- [ ] Integration: Update PetService status (reserve, adopt, release)
- [ ] Tests: State machine, handlers, API, event processing
- [ ] Docker: Dockerfile, add to compose.yaml (port 8084)

#### Application Lifecycle
- [ ] Match created → Application initiated (auto)
- [ ] Admin review → Approved/Rejected
- [ ] User completes process → Completed
- [ ] User cancels → Cancelled
- [ ] Each transition updates pet status in PetService

#### Definition of Done
- [ ] Service consumes `distribution.match-created.v1` and creates applications
- [ ] Users can create direct adoption applications (non-challenge)
- [ ] Admin can approve/reject applications
- [ ] Users can complete or cancel applications
- [ ] Events published to `adoption.events` exchange
- [ ] Integration with PetService to update pet status
- [ ] API running in Docker on port 8084
- [ ] Full adoption flow tested end-to-end
- [ ] Tests passing with 80%+ coverage

#### Blocked By
- [ ] DistributionService (need match-created events)
- [ ] PetService API (may need reserve/release endpoints)

---

## Phase 4: User Engagement

### NotificationService ⬜
**Status:** Not Started | **Priority:** P1 - High | **Effort:** 8-10 days

#### Prerequisites
- [ ] All other services running and publishing events
- [ ] Email provider configured (SendGrid or SMTP)

#### Core Tasks
- [ ] Create Background Worker project
- [ ] Domain: `Notification` entity, `NotificationTemplate` entity
- [ ] Application: Event handlers for all event types (8-10 handlers)
- [ ] Application: Email service, template renderer
- [ ] Infrastructure: MongoDB, RabbitMQ consumers (4 queues), email service
- [ ] Email templates: 6 Razor templates (registration, reminder, match, approval, rejection, completion)
- [ ] RabbitMQ bindings: Subscribe to all event exchanges with wildcards
- [ ] Configuration: SMTP/SendGrid settings
- [ ] Background processor: Retry failed notifications, cleanup
- [ ] Tests: Event handlers, template rendering, email sending
- [ ] Docker: Dockerfile, add to compose.yaml

#### Event Subscriptions
- [ ] `challenge.participant-registered.v1` → Registration confirmation
- [ ] `distribution.match-created.v1` → Match notification
- [ ] `adoption.application-created.v1` → Application notification
- [ ] `adoption.application-approved.v1` → Approval email
- [ ] `adoption.application-rejected.v1` → Rejection email
- [ ] `adoption.application-completed.v1` → Congratulations email

#### Email Templates
- [ ] Responsive design
- [ ] Include pet photos
- [ ] Clear call-to-action buttons
- [ ] Unsubscribe link (optional)

#### Definition of Done
- [ ] Service consumes events from all exchanges
- [ ] Sends emails for key events
- [ ] Templates render correctly with data
- [ ] Failed notifications are retried
- [ ] Notification history stored in database
- [ ] Running in Docker
- [ ] Tests passing for email sending and templating

#### Blocked By
- [ ] UserService (user email addresses)
- [ ] ChallengeService (challenge details)
- [ ] DistributionService (match details)
- [ ] AdoptionService (application status)

---

## Phase 5: Enhancements

### PetService Enhancements ⬜
**Status:** Not Started | **Priority:** P2 - Medium | **Effort:** 3-5 days

#### Tasks
- [ ] Add `ReservedForChallenge` status
- [ ] Add `ReserveForChallenge()` and `ReleaseFromChallenge()` methods
- [ ] Add events: `PetReservedForChallengeEvent`, `PetReleasedFromChallengeEvent`
- [ ] Add event consumers for `challenge.cancelled.v1` and `distribution.completed.v1`
- [ ] Add query endpoint: `GET /api/pets/available-for-challenge`
- [ ] Update tests

#### Blocked By
- [ ] ChallengeService (for event subscriptions)
- [ ] DistributionService (for event subscriptions)

---

### Shared Libraries ⬜
**Status:** Not Started | **Priority:** P2 - Medium | **Effort:** 5-7 days

#### Tasks
- [ ] Create `PetAdoption.Shared.Messaging` project
  - [ ] Move event base classes
  - [ ] Move RabbitMQ infrastructure
  - [ ] Create internal NuGet package
- [ ] Create `PetAdoption.Shared.Domain` project
  - [ ] Move common value objects (Email, PhoneNumber)
  - [ ] Move base entities (Entity, AggregateRoot, ValueObject)
  - [ ] Create internal NuGet package
- [ ] Create `PetAdoption.Shared.Infrastructure` project
  - [ ] Move Outbox pattern implementation
  - [ ] Move MongoDB helpers
  - [ ] Move HTTP client factories
  - [ ] Create internal NuGet package
- [ ] Refactor all services to use shared libraries
- [ ] Test everything still works

#### Blocked By
- [ ] At least 2-3 services completed (to identify common patterns)

---

### Preference-Based Matching ⬜
**Status:** Not Started | **Priority:** P3 - Low | **Effort:** 5-7 days

#### Tasks
- [ ] Enhance `UserPreferences` value object
- [ ] Add pet attributes to PetService
- [ ] Implement `PreferenceBasedMatchingStrategy`
- [ ] Add scoring algorithm
- [ ] Update DistributionService to support multiple strategies
- [ ] Add strategy configuration
- [ ] Compare strategies with analytics
- [ ] Test matching quality

#### Blocked By
- [ ] DistributionService (basic matching working)
- [ ] Analytics for comparison

---

### Admin Dashboard & Analytics ⬜
**Status:** Not Started | **Priority:** P3 - Low | **Effort:** 10-15 days

#### Tasks
- [ ] Add admin endpoints to each service
- [ ] Create analytics queries and aggregations
- [ ] Build Blazor admin UI
- [ ] Dashboard with charts (participation, success rates, time-to-adoption)
- [ ] Challenge management UI
- [ ] User management UI
- [ ] Adoption review queue UI
- [ ] Reports generation

#### Blocked By
- [ ] All core services completed

---

## Infrastructure Checklist

### Docker Compose ⬜
- [ ] MongoDB (port 27017) ✅
- [ ] RabbitMQ (ports 5672, 15672) ✅
- [ ] PostgreSQL (port 5432) for Hangfire
- [ ] PetService (port 8080) ✅
- [ ] UserService (port 8081)
- [ ] ChallengeService (port 8082)
- [ ] SchedulerService (no HTTP port)
- [ ] DistributionService (port 8083)
- [ ] AdoptionService (port 8084)
- [ ] NotificationService (no HTTP port)
- [ ] Web UI (port 8090) - Optional

### RabbitMQ Exchanges ⬜
- [ ] `pet.events` (topic) ✅
- [ ] `user.events` (topic)
- [ ] `challenge.events` (topic)
- [ ] `distribution.events` (topic)
- [ ] `adoption.events` (topic)

### MongoDB Databases ⬜
- [ ] `PetAdoptionDb` (PetService) ✅
- [ ] `UserDb` (UserService)
- [ ] `ChallengeDb` (ChallengeService)
- [ ] `DistributionDb` (DistributionService)
- [ ] `AdoptionDb` (AdoptionService)
- [ ] `NotificationDb` (NotificationService)

### PostgreSQL Databases ⬜
- [ ] `HangfireDb` (SchedulerService)

---

## Testing Checklist

### Unit Tests ⬜
- [ ] UserService domain + handlers
- [ ] ChallengeService domain + handlers
- [ ] DistributionService domain + matching algorithm
- [ ] AdoptionService domain + state machine
- [ ] NotificationService handlers + templates
- [ ] Target: 80%+ coverage per service

### Integration Tests ⬜
- [ ] UserService API with MongoDB
- [ ] ChallengeService API + RabbitMQ events
- [ ] SchedulerService job execution
- [ ] DistributionService event processing
- [ ] AdoptionService lifecycle
- [ ] NotificationService email sending
- [ ] Use Testcontainers for infrastructure

### End-to-End Tests ⬜
- [ ] Complete challenge flow
  1. Admin creates challenge
  2. Users register (10 users)
  3. Scheduler triggers at scheduled time
  4. Distribution creates matches
  5. Adoption applications created
  6. Admin approves applications
  7. Users complete adoptions
  8. Notifications sent at each step
- [ ] Failure scenarios
  - [ ] Service unavailable during distribution
  - [ ] Insufficient pets scenario
  - [ ] Event delivery failure and retry
  - [ ] Database connection loss

---

## Progress Tracking

### Overall Progress
- **Services Completed:** 1/7 (14%)
- **Current Phase:** Phase 0 (Setup) → Phase 1 (Foundation)
- **Next Milestone:** UserService + ChallengeService

### Service Status Summary

| Service | Status | Phase | Priority | Effort | Blocked By |
|---------|--------|-------|----------|--------|------------|
| PetService | ✅ Done | - | - | - | - |
| UserService | ⬜ Not Started | 1 | P0 | 5-8 days | None |
| ChallengeService | ⬜ Not Started | 1 | P0 | 8-10 days | UserService |
| SchedulerService | ⬜ Not Started | 2 | P0 | 5-7 days | ChallengeService, PostgreSQL |
| DistributionService | ⬜ Not Started | 3 | P0 | 10-12 days | SchedulerService, ChallengeService |
| AdoptionService | ⬜ Not Started | 3 | P0 | 8-10 days | DistributionService |
| NotificationService | ⬜ Not Started | 4 | P1 | 8-10 days | All services |

### Timeline
- **MVP Target (Phases 1-4):** 8-12 weeks
- **Full System (Phases 1-5):** 12-18 weeks
- **Current Sprint:** Sprint 0 (Planning)
- **Next Sprint:** Sprint 1 (UserService + ChallengeService)

---

## Quick Commands

### Start Development
```bash
# Start infrastructure only
docker compose up mongo rabbitmq postgres

# Run a specific service
dotnet run --project src/Services/UserService/PetAdoption.UserService.API

# Run all services
docker compose up --build
```

### Testing
```bash
# Run all tests
dotnet test PetAdoption.sln

# Run tests for specific service
dotnet test tests/UserService/PetAdoption.UserService.UnitTests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### RabbitMQ Management
- **URL:** http://localhost:15672
- **Username:** guest
- **Password:** guest

### MongoDB Compass
- **Connection String:** mongodb://root:example@localhost:27017

### Hangfire Dashboard (when SchedulerService running)
- **URL:** http://localhost:5000/hangfire (configure port in SchedulerService)

---

## Notes

### Key Success Factors
1. ✅ Follow dependency order strictly
2. ✅ Complete testing before moving to next service
3. ✅ Use Outbox pattern for reliable event publishing
4. ✅ Implement idempotency for all event handlers
5. ✅ Add comprehensive logging and error handling
6. ✅ Keep services loosely coupled via events

### Common Pitfalls to Avoid
1. ❌ Starting dependent services before their dependencies
2. ❌ Skipping tests to "move faster"
3. ❌ Sharing databases between services
4. ❌ Synchronous HTTP calls for critical flows (use events)
5. ❌ Forgetting event versioning (always use .v1)
6. ❌ Auto-acknowledging RabbitMQ messages before processing

---

**Last Updated:** 2026-02-20
**Next Review:** Weekly during implementation
**Owner:** Development Team
