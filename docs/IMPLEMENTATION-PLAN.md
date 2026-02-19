# PetAdoption Project - Implementation Plan

## Table of Contents
1. [Overview](#overview)
2. [Service Dependency Graph](#service-dependency-graph)
3. [Big Steps (Phases)](#big-steps-phases)
4. [Detailed Implementation Steps](#detailed-implementation-steps)
5. [Infrastructure Setup](#infrastructure-setup)
6. [Testing Strategy](#testing-strategy)
7. [Deployment Order](#deployment-order)

---

## Overview

This plan outlines the complete implementation of the PetAdoption microservices system, which enables users to participate in random pet adoption challenges where pets are automatically matched with participants at scheduled times.

**Current State:**
- ✅ PetService (fully implemented)
- ✅ MongoDB infrastructure
- ✅ RabbitMQ infrastructure
- ✅ Docker Compose setup
- ✅ Shared infrastructure code patterns

**Target State:**
- 7 microservices working together
- Event-driven architecture with RabbitMQ
- Complete adoption challenge feature flow
- Notification system for user engagement

---

## Service Dependency Graph

```
┌──────────────────────────────────────────────────────────────────┐
│                         DEPENDENCY LAYERS                         │
└──────────────────────────────────────────────────────────────────┘

LAYER 0 (Independent - No dependencies):
├── PetService ✅ (existing)
└── UserService

LAYER 1 (Depends on Layer 0):
├── ChallengeService → depends on: UserService
└── (can be developed in parallel)

LAYER 2 (Depends on Layer 0 & 1):
└── SchedulerService → depends on: ChallengeService

LAYER 3 (Depends on Layers 0, 1, 2):
└── DistributionService → depends on: ChallengeService, PetService, SchedulerService (events)

LAYER 4 (Depends on Layer 3):
└── AdoptionService → depends on: DistributionService

LAYER 5 (Cross-cutting - Depends on all):
└── NotificationService → depends on: ALL services (consumes all events)
```

**Critical Path:** UserService → ChallengeService → SchedulerService → DistributionService → AdoptionService

**Parallel Development Opportunities:**
- PetService enhancements can run in parallel with UserService
- NotificationService can be developed last as it's purely reactive

---

## Big Steps (Phases)

### Phase 1: Foundation Services (Layer 0-1)
**Goal:** Establish core domain services that others depend on
- **Duration Estimate:** Sprint 1-2
- **Services:** UserService, ChallengeService
- **Deliverables:** User management, Challenge CRUD, participant registration

### Phase 2: Scheduling Infrastructure (Layer 2)
**Goal:** Enable time-based triggers for distributions
- **Duration Estimate:** Sprint 2-3
- **Services:** SchedulerService
- **Deliverables:** Automated challenge triggering at scheduled times

### Phase 3: Core Feature Flow (Layer 3-4)
**Goal:** Complete the matching and adoption workflow
- **Duration Estimate:** Sprint 3-4
- **Services:** DistributionService, AdoptionService
- **Deliverables:** Pet-to-participant matching, adoption application workflow

### Phase 4: User Engagement (Layer 5)
**Goal:** Keep users informed throughout the process
- **Duration Estimate:** Sprint 4-5
- **Services:** NotificationService
- **Deliverables:** Email notifications, in-app notifications

### Phase 5: Enhancement & Refinement
**Goal:** Improve the system with advanced features
- **Duration Estimate:** Sprint 5+
- **Deliverables:** Preference-based matching, analytics, recurring challenges

---

## Detailed Implementation Steps

### PHASE 1: Foundation Services

#### 1.1 UserService Implementation
**Dependencies:** None
**Estimated Effort:** 5-8 days

##### Step 1.1.1: Project Setup
- [ ] Create solution structure following Clean Architecture
  ```bash
  dotnet new sln -n PetAdoption.UserService -o src/Services/UserService
  dotnet new webapi -n PetAdoption.UserService.API
  dotnet new classlib -n PetAdoption.UserService.Application
  dotnet new classlib -n PetAdoption.UserService.Domain
  dotnet new classlib -n PetAdoption.UserService.Infrastructure
  ```
- [ ] Add project references (API → Infrastructure → Application → Domain)
- [ ] Add to main solution: `dotnet sln PetAdoption.sln add src/Services/UserService/**/*.csproj`

##### Step 1.1.2: Domain Layer
- [ ] Create `User` aggregate root
  - Properties: `UserId`, `Email`, `FullName`, `PhoneNumber`, `UserStatus` (Active, Suspended)
  - Methods: `Create()`, `UpdateProfile()`, `Suspend()`, `Activate()`
- [ ] Create value objects
  - `Email` (with validation)
  - `FullName` (max 100 chars)
  - `PhoneNumber` (optional, with format validation)
- [ ] Create domain events
  - `UserRegisteredEvent`
  - `UserProfileUpdatedEvent`
  - `UserSuspendedEvent`
- [ ] Create `IUserRepository` interface
- [ ] Create domain exceptions: `UserNotFoundException`, `DuplicateEmailException`

##### Step 1.1.3: Application Layer
- [ ] Create commands
  - `RegisterUserCommand` + handler
  - `UpdateUserProfileCommand` + handler
  - `SuspendUserCommand` + handler
- [ ] Create queries
  - `GetUserByIdQuery` + handler
  - `GetUserByEmailQuery` + handler
  - `GetAllUsersQuery` + handler (with pagination)
- [ ] Create DTOs: `UserDto`, `UserListItemDto`, `RegisterUserResponse`

##### Step 1.1.4: Infrastructure Layer
- [ ] Copy RabbitMQ topology setup from PetService
- [ ] Implement `UserRepository` (MongoDB)
- [ ] Implement `UserQueryStore` (MongoDB)
- [ ] Implement `OutboxRepository` (for reliable event publishing)
- [ ] Configure MongoDB connection and collections
- [ ] Implement event publisher
- [ ] Create background service `OutboxProcessorService`

##### Step 1.1.5: API Layer
- [ ] Create `UsersController` with endpoints
  - `POST /api/users` - Register user
  - `GET /api/users/{id}` - Get user by ID
  - `GET /api/users` - List users (paginated)
  - `PUT /api/users/{id}` - Update user profile
  - `POST /api/users/{id}/suspend` - Suspend user
- [ ] Configure Swagger/OpenAPI
- [ ] Add exception handling middleware
- [ ] Configure appsettings.json with MongoDB and RabbitMQ

##### Step 1.1.6: Testing
- [ ] Create unit tests for User aggregate
- [ ] Create unit tests for command/query handlers
- [ ] Create integration tests for API endpoints

##### Step 1.1.7: Infrastructure
- [ ] Create Dockerfile
- [ ] Add to compose.yaml (port 8081)
- [ ] Configure RabbitMQ exchange: `user.events` (topic)
- [ ] Configure queues for user events

---

#### 1.2 ChallengeService Implementation
**Dependencies:** UserService (for user ID references)
**Estimated Effort:** 8-10 days

##### Step 1.2.1: Project Setup
- [ ] Create solution structure (same as UserService)
- [ ] Add project references
- [ ] Add to main solution

##### Step 1.2.2: Domain Layer
- [ ] Create `AdoptionChallenge` aggregate root
  - Properties: `ChallengeId`, `Title`, `Description`, `DistributionDateTime`, `ChallengeStatus`, `MaxParticipants`, `MinPetsRequired`
  - Status: Draft, Open, Scheduled, InProgress, Completed, Cancelled
  - Methods: `Create()`, `Open()`, `Schedule()`, `Start()`, `Complete()`, `Cancel()`
- [ ] Create `Participant` entity
  - Properties: `ParticipantId`, `UserId`, `ChallengeId`, `RegisteredAt`, `UserPreferences`
- [ ] Create value objects
  - `ChallengeId`
  - `ParticipantId`
  - `UserPreferences` (petType, size, age preferences)
- [ ] Create domain events
  - `ChallengeCreatedEvent`
  - `ChallengeOpenedEvent`
  - `ParticipantRegisteredEvent`
  - `ParticipantUnregisteredEvent`
  - `ChallengeReadyForDistributionEvent`
  - `ChallengeCompletedEvent`
  - `ChallengeCancelledEvent`
- [ ] Create repositories: `IChallengeRepository`, `IParticipantRepository`
- [ ] Create domain exceptions

##### Step 1.2.3: Application Layer
- [ ] Create commands
  - `CreateChallengeCommand` + handler
  - `OpenChallengeCommand` + handler
  - `RegisterForChallengeCommand` + handler
  - `UnregisterFromChallengeCommand` + handler
  - `CancelChallengeCommand` + handler
  - `CompleteChallengeCommand` + handler
- [ ] Create queries
  - `GetChallengeByIdQuery` + handler
  - `GetActiveChallengesQuery` + handler
  - `GetChallengeParticipantsQuery` + handler
  - `GetUserChallengesQuery` + handler
- [ ] Create DTOs
- [ ] Add business rules validation
  - Max participants check
  - Registration deadline check
  - Duplicate registration check

##### Step 1.2.4: Infrastructure Layer
- [ ] Implement `ChallengeRepository` (MongoDB)
- [ ] Implement `ParticipantRepository` (MongoDB)
- [ ] Implement `ChallengeQueryStore` (MongoDB)
- [ ] Implement `OutboxRepository`
- [ ] Create MongoDB collections: `Challenges`, `Participants`
- [ ] Configure RabbitMQ exchange: `challenge.events` (topic)
- [ ] Implement outbox processor

##### Step 1.2.5: API Layer
- [ ] Create `ChallengesController`
  - `POST /api/challenges` - Create challenge (admin only)
  - `GET /api/challenges` - List active challenges
  - `GET /api/challenges/{id}` - Get challenge details
  - `POST /api/challenges/{id}/open` - Open for registration
  - `POST /api/challenges/{id}/register` - Register participant
  - `DELETE /api/challenges/{id}/register` - Unregister
  - `GET /api/challenges/{id}/participants` - List participants
  - `POST /api/challenges/{id}/cancel` - Cancel challenge
- [ ] Add authorization (admin vs regular user)
- [ ] Configure Swagger

##### Step 1.2.6: Integration with UserService
- [ ] Add HTTP client to call UserService for user validation
- [ ] Handle cases where user doesn't exist
- [ ] Add retry policies for resilience

##### Step 1.2.7: Testing
- [ ] Unit tests for domain logic
- [ ] Unit tests for handlers
- [ ] Integration tests with MongoDB
- [ ] Integration tests for event publishing

##### Step 1.2.8: Infrastructure
- [ ] Create Dockerfile
- [ ] Add to compose.yaml (port 8082)
- [ ] Configure RabbitMQ topology

---

### PHASE 2: Scheduling Infrastructure

#### 2.1 SchedulerService Implementation
**Dependencies:** ChallengeService (consumes challenge.created.v1, queries challenge data)
**Estimated Effort:** 5-7 days

##### Step 2.1.1: Technology Selection
- [ ] Decide on scheduler implementation
  - **Option A:** Hangfire (recommended - mature, persistent, dashboard)
  - **Option B:** Quartz.NET (enterprise-grade)
  - **Option C:** Custom with Timer + MongoDB (lightweight)
- [ ] For MVP: Use Hangfire with PostgreSQL for job persistence

##### Step 2.1.2: Project Setup
- [ ] Create Background Worker service (not Web API)
  ```bash
  dotnet new worker -n PetAdoption.SchedulerService
  ```
- [ ] Add NuGet packages
  - `Hangfire.Core`
  - `Hangfire.PostgreSql` (or `Hangfire.Mongo`)
  - `RabbitMQ.Client`
- [ ] Add to main solution

##### Step 2.1.3: Domain Layer (Minimal)
- [ ] Create `ScheduledJob` entity
  - Properties: `JobId`, `ChallengeId`, `ScheduledTime`, `Status` (Scheduled, Triggered, Cancelled, Failed)
- [ ] Create `IScheduledJobRepository` interface

##### Step 2.1.4: Application Layer
- [ ] Create job scheduler service
  - `IJobScheduler` interface
  - `HangfireJobScheduler` implementation
- [ ] Create event handlers
  - `ChallengeCreatedEventHandler` - schedules distribution job
  - `ChallengeCancelledEventHandler` - cancels scheduled job
- [ ] Create distribution trigger job
  - `TriggerDistributionJob` - publishes `challenge.ready-for-distribution.v1`

##### Step 2.1.5: Infrastructure Layer
- [ ] Configure Hangfire with PostgreSQL
  ```csharp
  services.AddHangfire(config => config
      .UsePostgreSqlStorage(connectionString));
  services.AddHangfireServer();
  ```
- [ ] Implement RabbitMQ consumer for challenge events
  - Subscribe to `challenge.events` exchange
  - Queue: `scheduler.challenge-created-handler`
  - Routing key: `challenge.created.v1`, `challenge.cancelled.v1`
- [ ] Implement RabbitMQ publisher for distribution triggers
  - Publish to `challenge.events` exchange
  - Routing key: `challenge.ready-for-distribution.v1`

##### Step 2.1.6: Worker Service Configuration
- [ ] Configure Worker service in `Program.cs`
- [ ] Add health checks
- [ ] Configure logging
- [ ] Add graceful shutdown handling

##### Step 2.1.7: Testing
- [ ] Unit tests for job scheduling logic
- [ ] Integration tests with PostgreSQL and Hangfire
- [ ] Test job cancellation
- [ ] Test missed jobs (recovery)

##### Step 2.1.8: Infrastructure
- [ ] Add PostgreSQL to compose.yaml (for Hangfire persistence)
  ```yaml
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: HangfireDb
      POSTGRES_USER: hangfire
      POSTGRES_PASSWORD: hangfire
  ```
- [ ] Create Dockerfile for worker service
- [ ] Add scheduler service to compose.yaml (no HTTP port needed)

---

### PHASE 3: Core Feature Flow

#### 3.1 DistributionService Implementation
**Dependencies:** ChallengeService, PetService, SchedulerService (via events)
**Estimated Effort:** 10-12 days

##### Step 3.1.1: Project Setup
- [ ] Create solution structure (Web API + layers)
- [ ] Add project references
- [ ] Add to main solution

##### Step 3.1.2: Domain Layer
- [ ] Create `Distribution` aggregate root
  - Properties: `DistributionId`, `ChallengeId`, `ScheduledAt`, `ExecutedAt`, `Status`, `Matches`
  - Status: Scheduled, Running, Completed, Failed
  - Methods: `Create()`, `Start()`, `AddMatch()`, `Complete()`, `Fail()`
- [ ] Create `Match` entity
  - Properties: `MatchId`, `PetId`, `ParticipantId`, `MatchScore`, `CreatedAt`
- [ ] Create value objects: `DistributionId`, `MatchId`
- [ ] Create matching strategy interfaces
  - `IMatchingStrategy` (abstraction)
  - `RandomMatchingStrategy` (MVP implementation)
  - `PreferenceBasedMatchingStrategy` (future)
- [ ] Create domain events
  - `DistributionStartedEvent`
  - `MatchCreatedEvent`
  - `DistributionCompletedEvent`
  - `DistributionFailedEvent`
- [ ] Create repositories: `IDistributionRepository`, `IMatchRepository`

##### Step 3.1.3: Application Layer
- [ ] Create commands
  - `StartDistributionCommand` + handler (triggered by event)
  - `RetryDistributionCommand` + handler (manual retry)
- [ ] Create queries
  - `GetDistributionByIdQuery` + handler
  - `GetDistributionByChallengeIdQuery` + handler
  - `GetMatchesByDistributionIdQuery` + handler
- [ ] Create external service clients
  - `IChallengeServiceClient` - fetch participants and pet list
  - `IPetServiceClient` - fetch available pets
- [ ] Implement matching algorithm
  - Random shuffle algorithm (Fisher-Yates)
  - Handle edge cases (more participants than pets, vice versa)
  - Calculate basic match score

##### Step 3.1.4: Infrastructure Layer
- [ ] Implement repositories (MongoDB)
  - Collections: `Distributions`, `Matches`
- [ ] Implement HTTP clients for external services
  - ChallengeService client with retry policy
  - PetService client with retry policy
- [ ] Implement RabbitMQ consumer
  - Queue: `distribution.challenge-ready-handler`
  - Routing key: `challenge.ready-for-distribution.v1`
- [ ] Implement RabbitMQ publisher
  - Exchange: `distribution.events`
  - Publish match-created events
- [ ] Implement outbox processor

##### Step 3.1.5: API Layer (Optional - mostly event-driven)
- [ ] Create `DistributionsController` (admin/monitoring)
  - `GET /api/distributions/{id}` - Get distribution details
  - `GET /api/distributions/challenge/{challengeId}` - Get by challenge
  - `POST /api/distributions/{id}/retry` - Manual retry
- [ ] Add Swagger

##### Step 3.1.6: Event Handlers
- [ ] Implement `ChallengeReadyForDistributionEventHandler`
  - Receives event from SchedulerService
  - Fetches participants from ChallengeService
  - Fetches available pets from PetService
  - Runs matching algorithm
  - Saves distribution and matches
  - Publishes `MatchCreatedEvent` for each match
  - Publishes `DistributionCompletedEvent`

##### Step 3.1.7: Error Handling
- [ ] Handle insufficient pets scenario
- [ ] Handle service unavailability (ChallengeService, PetService down)
- [ ] Implement idempotency (handle duplicate events)
- [ ] Add comprehensive logging

##### Step 3.1.8: Testing
- [ ] Unit tests for matching algorithms
- [ ] Unit tests for domain logic
- [ ] Integration tests with mock services
- [ ] Integration tests with real RabbitMQ
- [ ] End-to-end test: challenge creation → distribution → matches

##### Step 3.1.9: Infrastructure
- [ ] Create Dockerfile
- [ ] Add to compose.yaml (port 8083)
- [ ] Configure RabbitMQ topology

---

#### 3.2 AdoptionService Implementation
**Dependencies:** DistributionService (consumes distribution.match-created.v1), PetService
**Estimated Effort:** 8-10 days

##### Step 3.2.1: Project Setup
- [ ] Create solution structure (Web API + layers)
- [ ] Add project references
- [ ] Add to main solution

##### Step 3.2.2: Domain Layer
- [ ] Create `AdoptionApplication` aggregate root
  - Properties: `ApplicationId`, `PetId`, `UserId`, `Source` (Challenge/Direct), `MatchId`, `Status`, `CreatedAt`, `ApprovedAt`, `CompletedAt`, `RejectedAt`, `RejectionReason`
  - Status: Initiated, UnderReview, Approved, Rejected, Completed, Cancelled
  - Methods: `CreateFromMatch()`, `CreateDirect()`, `Approve()`, `Reject()`, `Complete()`, `Cancel()`
- [ ] Create value objects
  - `ApplicationId`
  - `ApplicationSource` (enum: Challenge, Direct)
- [ ] Create domain events
  - `AdoptionApplicationCreatedEvent`
  - `AdoptionApplicationApprovedEvent`
  - `AdoptionApplicationRejectedEvent`
  - `AdoptionApplicationCompletedEvent`
  - `AdoptionApplicationCancelledEvent`
- [ ] Create repository: `IAdoptionApplicationRepository`
- [ ] Create domain business rules
  - User can only have N active applications
  - Same user can't apply for same pet twice

##### Step 3.2.3: Application Layer
- [ ] Create commands
  - `CreateApplicationFromMatchCommand` + handler (from distribution)
  - `CreateDirectApplicationCommand` + handler (manual adoption)
  - `ApproveApplicationCommand` + handler (admin)
  - `RejectApplicationCommand` + handler (admin)
  - `CompleteApplicationCommand` + handler (after payment/paperwork)
  - `CancelApplicationCommand` + handler (user cancels)
- [ ] Create queries
  - `GetApplicationByIdQuery` + handler
  - `GetApplicationsByUserIdQuery` + handler
  - `GetApplicationsByPetIdQuery` + handler
  - `GetApplicationsByStatusQuery` + handler (admin dashboard)
  - `GetPendingApplicationsQuery` + handler (admin review queue)
- [ ] Create DTOs

##### Step 3.2.4: Infrastructure Layer
- [ ] Implement `AdoptionApplicationRepository` (MongoDB)
  - Collection: `AdoptionApplications`
  - Indexes: userId, petId, status, createdAt
- [ ] Implement query store
- [ ] Implement RabbitMQ consumer
  - Queue: `adoption.match-created-handler`
  - Routing key: `distribution.match-created.v1`
- [ ] Implement RabbitMQ publisher
  - Exchange: `adoption.events`
- [ ] Implement outbox processor
- [ ] Implement HTTP client for PetService (to update pet status)

##### Step 3.2.5: API Layer
- [ ] Create `AdoptionsController`
  - `POST /api/adoptions/applications` - Create direct application
  - `GET /api/adoptions/applications` - List all (filtered by user/status)
  - `GET /api/adoptions/applications/{id}` - Get details
  - `PUT /api/adoptions/applications/{id}/approve` - Approve (admin)
  - `PUT /api/adoptions/applications/{id}/reject` - Reject (admin)
  - `PUT /api/adoptions/applications/{id}/complete` - Complete
  - `DELETE /api/adoptions/applications/{id}` - Cancel
- [ ] Add authorization (user vs admin)
- [ ] Configure Swagger

##### Step 3.2.6: Event Handlers
- [ ] Implement `MatchCreatedEventHandler`
  - Receives match from DistributionService
  - Creates adoption application automatically
  - Sets source as "Challenge"
  - Sets status as "Initiated"
  - Publishes `AdoptionApplicationCreatedEvent`

##### Step 3.2.7: Integration with PetService
- [ ] When application approved → call PetService to reserve pet
- [ ] When application completed → call PetService to mark as adopted
- [ ] When application rejected/cancelled → call PetService to release pet

##### Step 3.2.8: Testing
- [ ] Unit tests for domain logic
- [ ] Unit tests for handlers
- [ ] Integration tests for API
- [ ] Integration tests for event processing
- [ ] Test application lifecycle state machine

##### Step 3.2.9: Infrastructure
- [ ] Create Dockerfile
- [ ] Add to compose.yaml (port 8084)
- [ ] Configure RabbitMQ topology

---

### PHASE 4: User Engagement

#### 4.1 NotificationService Implementation
**Dependencies:** ALL services (consumes events from all)
**Estimated Effort:** 8-10 days

##### Step 4.1.1: Project Setup
- [ ] Create Background Worker service (no Web API needed)
- [ ] Add NuGet packages
  - Email: `MailKit` or `SendGrid` SDK
  - Templates: `RazorLight` for email templates
  - RabbitMQ: `RabbitMQ.Client`

##### Step 4.1.2: Domain Layer (Simple)
- [ ] Create `Notification` entity
  - Properties: `NotificationId`, `UserId`, `Type`, `Channel`, `Subject`, `Body`, `Status`, `SentAt`, `FailedAt`, `RetryCount`
  - Type: ChallengeRegistration, ChallengeReminder, MatchCreated, AdoptionApproved, etc.
  - Channel: Email, SMS, Push, InApp
  - Status: Pending, Sent, Failed, Cancelled
- [ ] Create `NotificationTemplate` entity
  - Properties: `TemplateId`, `Name`, `Subject`, `BodyTemplate`, `Channel`
- [ ] Create repository: `INotificationRepository`

##### Step 4.1.3: Application Layer
- [ ] Create notification handlers for each event type
  - `ParticipantRegisteredEventHandler` → Send registration confirmation
  - `MatchCreatedEventHandler` → Send "You got matched!" email
  - `AdoptionApplicationCreatedEventHandler` → Send notification
  - `AdoptionApplicationApprovedEventHandler` → Send approval email
  - `AdoptionApplicationRejectedEventHandler` → Send rejection email
  - `AdoptionApplicationCompletedEventHandler` → Send congratulations
- [ ] Create notification services
  - `IEmailService` interface
  - `MailKitEmailService` implementation
  - `ITemplateRenderer` interface
  - `RazorTemplateRenderer` implementation
- [ ] Create template management
  - Templates stored as embedded resources or in MongoDB

##### Step 4.1.4: Infrastructure Layer
- [ ] Implement `NotificationRepository` (MongoDB)
  - Collection: `Notifications`
  - Collection: `NotificationTemplates`
- [ ] Implement email service
  - Configure SMTP settings (use SendGrid/MailKit)
  - Handle email sending failures
  - Implement retry logic
- [ ] Implement RabbitMQ consumers
  - Queue: `notification.challenge-events`
    - Bindings: `challenge.events` exchange, routing key `challenge.#`
  - Queue: `notification.distribution-events`
    - Bindings: `distribution.events` exchange, routing key `distribution.#`
  - Queue: `notification.adoption-events`
    - Bindings: `adoption.events` exchange, routing key `adoption.#`
  - Queue: `notification.user-events`
    - Bindings: `user.events` exchange, routing key `user.#`
- [ ] Implement background processor
  - Process pending notifications from queue
  - Retry failed notifications
  - Clean up old notifications

##### Step 4.1.5: Email Templates
- [ ] Create Razor templates
  - `ChallengeRegistrationConfirmation.cshtml`
  - `ChallengeReminderEmail.cshtml` (24h, 1h before)
  - `MatchCreatedEmail.cshtml`
  - `AdoptionApprovedEmail.cshtml`
  - `AdoptionRejectedEmail.cshtml`
  - `AdoptionCompletedEmail.cshtml`
- [ ] Use responsive email design
- [ ] Include pet photos, challenge details, links

##### Step 4.1.6: Configuration
- [ ] Configure SMTP settings in appsettings
  ```json
  {
    "Email": {
      "Provider": "SendGrid",
      "SendGrid": {
        "ApiKey": "...",
        "FromEmail": "noreply@petadoption.com",
        "FromName": "Pet Adoption"
      }
    }
  }
  ```
- [ ] Configure notification preferences
- [ ] Add feature flags for notification channels

##### Step 4.1.7: Testing
- [ ] Unit tests for notification handlers
- [ ] Unit tests for template rendering
- [ ] Integration tests with RabbitMQ
- [ ] Email sending tests (use test SMTP or mock)

##### Step 4.1.8: Infrastructure
- [ ] Create Dockerfile
- [ ] Add to compose.yaml (no HTTP port)
- [ ] Configure RabbitMQ bindings

##### Step 4.1.9: Enhancements (Optional)
- [ ] Add SMS notifications (Twilio)
- [ ] Add push notifications (Firebase)
- [ ] Add in-app notifications (WebSocket/SignalR)
- [ ] Add notification preferences API
- [ ] Add unsubscribe functionality

---

### PHASE 5: Enhancement & Refinement

#### 5.1 PetService Enhancements
**Estimated Effort:** 3-5 days

- [ ] Add pet reservation for challenges
  - New status: `ReservedForChallenge`
  - Methods: `ReserveForChallenge()`, `ReleaseFromChallenge()`
- [ ] Add new domain events
  - `PetReservedForChallengeEvent`
  - `PetReleasedFromChallengeEvent`
- [ ] Add event consumers
  - Listen to `challenge.cancelled.v1` → release reserved pets
  - Listen to `distribution.completed.v1` → mark pets as reserved for specific users
- [ ] Add query endpoints
  - `GET /api/pets/available-for-challenge` - Filter by availability
- [ ] Update tests

#### 5.2 Shared Libraries Creation
**Estimated Effort:** 5-7 days

##### Step 5.2.1: Shared.Messaging
- [ ] Create project: `PetAdoption.Shared.Messaging`
- [ ] Move common event definitions
  - Base classes: `DomainEventBase`, `IntegrationEvent`
  - Event contracts: interfaces for all events
- [ ] Move RabbitMQ infrastructure
  - `RabbitMqTopologyBuilder`
  - `RabbitMqTopologySetup`
  - `RabbitMqOptions`
  - `IEventPublisher`, `RabbitMqEventPublisher`
- [ ] Create NuGet package (internal)

##### Step 5.2.2: Shared.Domain
- [ ] Create project: `PetAdoption.Shared.Domain`
- [ ] Move common value objects
  - `Email`
  - `PhoneNumber`
  - Common IDs (if using GUID patterns)
- [ ] Move base entities
  - `Entity<T>`
  - `AggregateRoot`
  - `ValueObject`
- [ ] Create NuGet package (internal)

##### Step 5.2.3: Shared.Infrastructure
- [ ] Create project: `PetAdoption.Shared.Infrastructure`
- [ ] Move common infrastructure
  - Outbox pattern: `OutboxEvent`, `OutboxRepository`, `OutboxProcessor`
  - MongoDB helpers: `MongoDbContext`, `IMongoRepository<T>`
  - HTTP client factories with retry policies
  - Mediator pattern abstractions
- [ ] Create NuGet package (internal)

##### Step 5.2.4: Refactor Services
- [ ] Update all services to use shared libraries
- [ ] Remove duplicate code
- [ ] Test everything still works

#### 5.3 Preference-Based Matching
**Estimated Effort:** 5-7 days

- [ ] Enhance `UserPreferences` value object
  - Add detailed preferences: petType, size, age, breed, color, energy level
- [ ] Implement `PreferenceBasedMatchingStrategy`
  - Scoring algorithm based on preferences
  - Weighted scoring (critical vs nice-to-have)
- [ ] Add pet attributes to PetService
  - Size, age, breed, color, energy level, temperament
- [ ] Update DistributionService to use new strategy
- [ ] Add A/B testing framework to compare strategies
- [ ] Analytics: track match success rate by strategy

#### 5.4 Admin Dashboard & Analytics
**Estimated Effort:** 10-15 days

- [ ] Create admin API endpoints in each service
  - Challenge management
  - User management
  - Adoption application review
- [ ] Create analytics queries
  - Challenge participation rates
  - Match success rate
  - Adoption completion rate
  - Popular pet types
  - Time-to-adoption metrics
- [ ] Build admin UI (Blazor)
  - Dashboard with charts
  - Challenge management
  - User management
  - Adoption review queue
  - Reports

#### 5.5 Recurring Challenges
**Estimated Effort:** 5-7 days

- [ ] Extend `AdoptionChallenge` aggregate
  - Add `RecurrencePattern` (Daily, Weekly, Monthly)
  - Add `RecurrenceEndDate`
- [ ] Update SchedulerService
  - Support recurring job scheduling
  - Handle recurring challenge creation
- [ ] Update ChallengeService
  - Clone challenge for next occurrence
  - Track parent-child relationship

#### 5.6 Observability & Monitoring
**Estimated Effort:** 5-7 days

- [ ] Add structured logging (Serilog)
  - Configure sinks (Console, File, Seq)
  - Add correlation IDs across services
- [ ] Add distributed tracing (OpenTelemetry)
  - Trace requests across services
  - Jaeger or Zipkin integration
- [ ] Add metrics (Prometheus)
  - Business metrics: challenges created, matches made, adoptions completed
  - Technical metrics: request duration, error rate
  - RabbitMQ metrics: queue depth, message rate
- [ ] Add health checks
  - Database health
  - RabbitMQ health
  - Dependent service health
- [ ] Add monitoring dashboard (Grafana)
  - Service health overview
  - Business KPIs
  - System metrics

---

## Infrastructure Setup

### Development Environment

#### Required Software
- [ ] .NET 9 SDK
- [ ] Docker Desktop
- [ ] IDE: Visual Studio 2022 / Rider / VS Code
- [ ] MongoDB Compass (optional, for DB inspection)
- [ ] RabbitMQ Management UI (included in Docker)

#### Local Development Setup
```bash
# 1. Clone repository
git clone <repo-url>
cd PetAdoption

# 2. Start infrastructure
docker compose up mongo rabbitmq postgres

# 3. Run services individually
dotnet run --project src/Services/PetService/PetAdoption.PetService.API
dotnet run --project src/Services/UserService/PetAdoption.UserService.API
dotnet run --project src/Services/ChallengeService/PetAdoption.ChallengeService.API
# ... etc

# 4. Or run all via Docker Compose
docker compose up --build
```

### Docker Compose Configuration (Complete)

```yaml
services:
  # Infrastructure
  mongo:
    image: mongo:7.0
    ports: ["27017:27017"]
    # (existing config)

  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]
    # (existing config)

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: HangfireDb
      POSTGRES_USER: hangfire
      POSTGRES_PASSWORD: hangfire
    ports: ["5432:5432"]
    volumes:
      - postgres_data:/var/lib/postgresql/data

  # Services
  petservice:
    build: src/Services/PetService/PetAdoption.PetService.API
    ports: ["8080:8080"]
    depends_on: [mongo, rabbitmq]

  userservice:
    build: src/Services/UserService/PetAdoption.UserService.API
    ports: ["8081:8080"]
    depends_on: [mongo, rabbitmq]

  challengeservice:
    build: src/Services/ChallengeService/PetAdoption.ChallengeService.API
    ports: ["8082:8080"]
    depends_on: [mongo, rabbitmq, userservice]

  schedulerservice:
    build: src/Services/SchedulerService/PetAdoption.SchedulerService
    depends_on: [postgres, rabbitmq, challengeservice]

  distributionservice:
    build: src/Services/DistributionService/PetAdoption.DistributionService.API
    ports: ["8083:8080"]
    depends_on: [mongo, rabbitmq, challengeservice, petservice]

  adoptionservice:
    build: src/Services/AdoptionService/PetAdoption.AdoptionService.API
    ports: ["8084:8080"]
    depends_on: [mongo, rabbitmq, distributionservice, petservice]

  notificationservice:
    build: src/Services/NotificationService/PetAdoption.NotificationService
    depends_on: [mongo, rabbitmq]

  # Optional: Web UI
  webui:
    build: src/Web/PetAdoption.Web.BlazorApp
    ports: ["8090:8080"]
    depends_on:
      - petservice
      - userservice
      - challengeservice
      - adoptionservice

volumes:
  mongo_data:
  postgres_data:
```

### RabbitMQ Topology Summary

| Exchange | Type | Purpose |
|----------|------|---------|
| `pet.events` | topic | Pet domain events |
| `user.events` | topic | User domain events |
| `challenge.events` | topic | Challenge domain events |
| `distribution.events` | topic | Distribution and matching events |
| `adoption.events` | topic | Adoption application events |

**Key Routing Keys:**
- Pet: `pet.reserved.v1`, `pet.adopted.v1`, `pet.reservation.cancelled.v1`
- User: `user.registered.v1`, `user.profile.updated.v1`
- Challenge: `challenge.created.v1`, `challenge.opened.v1`, `challenge.participant-registered.v1`, `challenge.ready-for-distribution.v1`
- Distribution: `distribution.started.v1`, `distribution.match-created.v1`, `distribution.completed.v1`
- Adoption: `adoption.application-created.v1`, `adoption.application-approved.v1`, `adoption.application-completed.v1`

---

## Testing Strategy

### Unit Tests
**Coverage Target:** 80%+

- [ ] Domain layer: 100% coverage
  - Aggregate behavior
  - Value object validation
  - Domain event raising
- [ ] Application layer: 80%+ coverage
  - Command handlers
  - Query handlers
  - Business logic

### Integration Tests
**Coverage Target:** Critical paths

- [ ] API endpoints with real database
- [ ] Event publishing and consumption
- [ ] Cross-service communication
- [ ] Outbox pattern reliability
- [ ] Use Testcontainers for MongoDB and RabbitMQ

### End-to-End Tests
**Coverage Target:** Happy path + critical failures

- [ ] Complete challenge flow
  1. Admin creates challenge
  2. Users register
  3. Scheduler triggers distribution
  4. Matches created
  5. Adoption applications created
  6. Admin approves
  7. Adoption completed
  8. Notifications sent

- [ ] Failure scenarios
  - Service unavailable
  - Insufficient pets
  - Event delivery failures
  - Database connection issues

### Performance Tests
- [ ] Load test challenge registration (1000 users)
- [ ] Load test distribution algorithm (100 pets × 100 participants)
- [ ] Measure event processing latency
- [ ] Test system under RabbitMQ message backlog

---

## Deployment Order

### Development Environment
1. Infrastructure: MongoDB, RabbitMQ, PostgreSQL
2. Layer 0: PetService (existing), UserService
3. Layer 1: ChallengeService
4. Layer 2: SchedulerService
5. Layer 3: DistributionService
6. Layer 4: AdoptionService
7. Layer 5: NotificationService

### Production Deployment Strategy
**Recommended:** Blue-Green or Rolling deployment

1. Deploy infrastructure changes first (databases, queues)
2. Deploy services in dependency order (same as development)
3. Run database migrations before service deployment
4. Verify health checks after each service
5. Monitor RabbitMQ queues for message backlog
6. Use feature flags for new features
7. Keep backward compatibility for events (versioning)

---

## Risk Mitigation

### Technical Risks

| Risk | Mitigation |
|------|------------|
| **Service unavailability** | Implement circuit breakers, retry policies, health checks |
| **Event loss** | Use outbox pattern, RabbitMQ persistence, dead-letter queues |
| **Database connection issues** | Connection pooling, retry logic, failover |
| **Matching algorithm performance** | Load testing, optimize algorithm, async processing |
| **Race conditions** | Idempotency keys, optimistic concurrency, unique constraints |

### Business Risks

| Risk | Mitigation |
|------|------------|
| **Not enough pets** | Minimum pets requirement, cancel challenge, notify users |
| **Too many participants** | Maximum participants limit, waitlist, lottery system |
| **User dissatisfaction** | Clear communication, preferences, feedback system |
| **Low adoption completion** | Reminder notifications, incentives, streamlined process |

---

## Success Criteria

### Phase 1 (Foundation)
- ✅ UserService can register and manage users
- ✅ ChallengeService can create and manage challenges
- ✅ Users can register for challenges
- ✅ Events are reliably published to RabbitMQ

### Phase 2 (Scheduling)
- ✅ SchedulerService triggers distribution at scheduled time
- ✅ Jobs are persisted and survive restarts
- ✅ Cancelled challenges don't trigger distribution

### Phase 3 (Core Flow)
- ✅ DistributionService matches pets with participants
- ✅ AdoptionService creates applications from matches
- ✅ Complete flow from challenge creation to adoption
- ✅ All events are processed correctly

### Phase 4 (Engagement)
- ✅ NotificationService sends emails at key points
- ✅ Users receive timely notifications
- ✅ Email templates are professional and informative

### Phase 5 (Enhancement)
- ✅ Preference-based matching improves satisfaction
- ✅ Admin can manage system effectively
- ✅ Analytics provide business insights
- ✅ System is observable and monitorable

---

## Timeline Estimate

| Phase | Duration | Parallel Work |
|-------|----------|---------------|
| **Phase 1** | 2-3 weeks | UserService + ChallengeService (1 dev each) |
| **Phase 2** | 1-2 weeks | SchedulerService (1 dev) |
| **Phase 3** | 3-4 weeks | DistributionService + AdoptionService (sequential or 2 devs) |
| **Phase 4** | 2-3 weeks | NotificationService (1 dev) |
| **Phase 5** | 4-6 weeks | Various enhancements (team) |
| **Total** | **12-18 weeks** | With 2-3 developers |

**MVP (Phases 1-4):** 8-12 weeks
**Full System:** 12-18 weeks

---

## Next Steps

1. **Review this plan** with the team
2. **Set up project board** (Jira, Azure DevOps, GitHub Projects)
3. **Create epics and stories** from this plan
4. **Assign developers** to phases
5. **Start with Phase 1** - UserService and ChallengeService
6. **Hold weekly sync meetings** to track progress
7. **Update this document** as you learn and adapt

---

## Appendix: Command Reference

### Create New Service
```bash
# Navigate to Services directory
cd src/Services

# Create new service structure
dotnet new sln -n PetAdoption.ServiceName -o ServiceName
cd ServiceName

# Create projects
dotnet new webapi -n PetAdoption.ServiceName.API
dotnet new classlib -n PetAdoption.ServiceName.Application
dotnet new classlib -n PetAdoption.ServiceName.Domain
dotnet new classlib -n PetAdoption.ServiceName.Infrastructure

# Add to solution
dotnet sln add **/*.csproj

# Add project references
dotnet add API reference Application Infrastructure
dotnet add Application reference Domain
dotnet add Infrastructure reference Domain

# Add to main solution
cd ../../..
dotnet sln PetAdoption.sln add src/Services/ServiceName/**/*.csproj
```

### Run Individual Service
```bash
dotnet run --project src/Services/ServiceName/PetAdoption.ServiceName.API
```

### Run All Tests
```bash
dotnet test PetAdoption.sln --logger "console;verbosity=detailed"
```

### Build Docker Image
```bash
docker build -t petadoption.servicename \
  -f src/Services/ServiceName/PetAdoption.ServiceName.API/Dockerfile .
```

---

**Document Version:** 1.0
**Last Updated:** 2026-02-20
**Author:** Implementation Planning Team
**Status:** Draft - Pending Review
