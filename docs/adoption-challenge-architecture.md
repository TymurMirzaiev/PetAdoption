# Adoption Challenge - Service Architecture

## Services Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         USER INTERFACE                               │
│                      (Web App / Mobile App)                          │
└────────────┬────────────────────────────────────────────┬───────────┘
             │                                             │
             │ HTTP/REST                                   │ HTTP/REST
             │                                             │
┌────────────▼──────────┐                    ┌────────────▼──────────┐
│   CHALLENGE SERVICE   │                    │   ADOPTION SERVICE    │
│  ┌─────────────────┐  │                    │  ┌─────────────────┐  │
│  │ Create Challenge│  │                    │  │ Manage Applications│
│  │ User Registration│  │                    │  │ Approve/Reject  │  │
│  │ List Challenges  │  │                    │  │ Track Status    │  │
│  └─────────────────┘  │                    │  └─────────────────┘  │
│         MongoDB        │                    │       MongoDB         │
└────────────┬───────────┘                    └────────────▲──────────┘
             │                                             │
             │ Publishes events                            │ Consumes events
             │                                             │
             │        ┌────────────────────────────────────┘
             │        │
┌────────────▼────────▼──────────────────────────────────────────────┐
│                         RABBITMQ MESSAGE BROKER                      │
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │challenge.events│ │distribution. │  │adoption.     │             │
│  │   (topic)    │  │events (topic)│  │events (topic)│             │
│  └──────────────┘  └──────────────┘  └──────────────┘             │
│                                                                      │
│  Queues: challenge.*, distribution.*, adoption.*, notification.*    │
└───────────┬────────────────┬────────────────┬──────────────────────┘
            │                │                │
            │                │                │
            │                │                │
┌───────────▼───────┐ ┌──────▼─────────┐ ┌───▼──────────────────────┐
│ SCHEDULER SERVICE │ │ DISTRIBUTION   │ │  NOTIFICATION SERVICE    │
│  ┌─────────────┐  │ │    SERVICE     │ │   ┌─────────────────┐   │
│  │  Hangfire   │  │ │ ┌────────────┐ │ │   │ Email Handler   │   │
│  │  Monitor    │  │ │ │  Matching  │ │ │   │ Push Handler    │   │
│  │  Challenges │  │ │ │  Algorithm │ │ │   │ SMS Handler     │   │
│  │  Trigger at │  │ │ │  Random/   │ │ │   │                 │   │
│  │  Time       │  │ │ │  Preference│ │ │   └─────────────────┘   │
│  └─────────────┘  │ │ └────────────┘ │ │        MongoDB           │
│    SQL/MongoDB    │ │    MongoDB     │ └──────────────────────────┘
└───────────────────┘ └────────────────┘
            │                │
            │                │ Queries pet/user data
            │                │
            └────────────────┼────────────────────────┐
                             │                        │
                    ┌────────▼────────┐      ┌────────▼────────┐
                    │   PET SERVICE   │      │  USER SERVICE   │
                    │  (EXISTING)     │      │  (May exist)    │
                    │ ┌─────────────┐ │      │ ┌─────────────┐ │
                    │ │ Pet CRUD    │ │      │ │ User Profile│ │
                    │ │ Availability│ │      │ │ Preferences │ │
                    │ └─────────────┘ │      │ └─────────────┘ │
                    │    MongoDB      │      │    MongoDB      │
                    └─────────────────┘      └─────────────────┘
```

## Event Flow Diagram

### 1. Challenge Registration Flow
```
User → Challenge Service: POST /api/challenges/{id}/register
  │
  ├─> Challenge Service: Save participant to DB
  │
  └─> RabbitMQ: Publish "challenge.participant-registered.v1"
         │
         └─> Notification Service: Send confirmation email
```

### 2. Distribution Trigger Flow (The Core Feature)
```
Scheduler Service (runs every minute):
  │
  ├─> Check DB for challenges where distributionDateTime <= now
  │
  └─> For each ready challenge:
         │
         └─> RabbitMQ: Publish "challenge.ready-for-distribution.v1"
                │
                └─> Distribution Service receives event
                       │
                       ├─> Query Challenge Service API: Get participants
                       ├─> Query Pet Service API: Get available pets
                       │
                       ├─> Run matching algorithm
                       │   ┌───────────────────────────────────┐
                       │   │ Shuffle participants & pets       │
                       │   │ Match 1:1 until one list exhausted│
                       │   │ Generate Match entities           │
                       │   └───────────────────────────────────┘
                       │
                       ├─> Save Distribution to DB
                       │
                       └─> For each match:
                              │
                              └─> RabbitMQ: Publish "distribution.match-created.v1"
                                     │
                                     ├─> Adoption Service: Create application
                                     │      └─> RabbitMQ: "adoption.application-created.v1"
                                     │
                                     └─> Notification Service: "You got matched with [PetName]!"
```

### 3. Adoption Process Flow
```
Adoption Service receives "distribution.match-created.v1":
  │
  ├─> Create AdoptionApplication (status: Initiated)
  ├─> Save to DB
  └─> RabbitMQ: Publish "adoption.application-created.v1"
         │
         ├─> Notification Service: Notify user & admin
         └─> Pet Service: Mark pet as "Reserved"

Admin reviews application → PUT /api/adoptions/applications/{id}/approve
  │
  └─> Adoption Service:
         ├─> Update status to "Approved"
         └─> RabbitMQ: "adoption.application-approved.v1"
                │
                ├─> Pet Service: Mark as "Adopted"
                └─> Notification Service: "Congratulations! Adoption approved!"

User completes adoption → PUT /api/adoptions/applications/{id}/complete
  │
  └─> Adoption Service:
         ├─> Update status to "Completed"
         └─> RabbitMQ: "adoption.application-completed.v1"
                │
                └─> Analytics/Reporting services
```

## Service Responsibilities Summary

| Service | Main Responsibility | Publishes Events | Consumes Events |
|---------|---------------------|------------------|-----------------|
| **Challenge** | Manage challenges & registrations | challenge.* | - |
| **Scheduler** | Trigger time-based events | challenge.ready-for-distribution.v1 | challenge.created.v1 |
| **Distribution** | Match pets with participants | distribution.* | challenge.ready-for-distribution.v1 |
| **Adoption** | Track adoption lifecycle | adoption.* | distribution.match-created.v1 |
| **Notification** | Send notifications to users | - | *.* (all events) |
| **Pet** | Manage pet data & availability | pet.* | adoption.*, distribution.* |
| **User** | Manage user profiles | user.* | - |

## Data Flow Example

**Scenario:** Weekend Adoption Challenge with 10 participants and 8 pets

```
March 1, 10:00 AM - Admin creates challenge
  └─> Challenge Service: Create "Weekend Challenge" (distributionDateTime: March 15, 10:00 AM)

March 1-14 - Users register
  └─> 10 users register for the challenge

March 15, 10:00 AM - Scheduler triggers distribution
  └─> Scheduler: Detects it's time
      └─> Publishes: challenge.ready-for-distribution.v1

March 15, 10:00:03 AM - Distribution starts
  └─> Distribution Service:
      ├─> Fetches 10 participants
      ├─> Fetches 8 available pets
      ├─> Runs random matching: Creates 8 matches (2 participants unlucky)
      └─> Publishes 8 × "distribution.match-created.v1"

March 15, 10:00:05 AM - Adoption applications created
  └─> Adoption Service creates 8 applications (status: Initiated)

March 15, 10:00:06 AM - Notifications sent
  └─> 8 users receive: "You matched with Fluffy! Start your adoption."
  └─> 2 users receive: "Sorry, no match this time. Try next challenge!"
  └─> Admin receives: "8 matches created, ready for review"

March 15-20 - Adoption process
  └─> Admin reviews applications, approves/rejects
  └─> Users complete adoption steps
  └─> Pets get adopted
```

## Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Docker Compose / Kubernetes          │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Challenge │  │Distribution│ │ Adoption │            │
│  │ Service  │  │  Service   │ │ Service  │            │
│  │ :8081    │  │  :8082     │ │ :8083    │            │
│  └──────────┘  └──────────┘  └──────────┘            │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Scheduler │  │Notification│ │   Pet    │            │
│  │ Service  │  │  Service   │ │ Service  │            │
│  │(Worker)  │  │ (Worker)   │ │ :8080    │            │
│  └──────────┘  └──────────┘  └──────────┘            │
│                                                         │
│  ┌──────────────────────────────────────────┐         │
│  │         RabbitMQ  :5672, :15672          │         │
│  └──────────────────────────────────────────┘         │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │ MongoDB  │  │PostgreSQL│  │  Redis   │            │
│  │  :27017  │  │ :5432    │  │  :6379   │            │
│  │(Services)│  │(Hangfire)│  │ (Cache)  │            │
│  └──────────┘  └──────────┘  └──────────┘            │
└─────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### Why Separate Services?

1. **Challenge Service** - Bounded context: Managing challenges is different from managing pets
2. **Distribution Service** - Complex algorithm, can be scaled independently, may take time
3. **Scheduler Service** - Specialized responsibility, needs reliable job persistence
4. **Adoption Service** - Separate workflow, may involve payment, background checks, etc.
5. **Notification Service** - Cross-cutting concern, used by all services

### Why Event-Driven Architecture?

✅ **Loose Coupling**: Services don't need to know about each other
✅ **Scalability**: Can scale services independently
✅ **Resilience**: If Notification Service is down, adoptions still work
✅ **Extensibility**: Easy to add new services (e.g., Analytics Service)
✅ **Auditability**: Event log provides complete history

### Why Topic Exchange?

- Flexible routing with wildcards
- Services can subscribe to specific events or all events
- Versioning support (routing keys include .v1, .v2)
- Multiple consumers can receive same event

## Next Steps to Implement

1. ✅ Pet Service (already exists)
2. ⬜ Challenge Service - Start here (most critical)
3. ⬜ Simple Scheduler - Just a background worker with Timer
4. ⬜ Distribution Service - Random matching algorithm
5. ⬜ Adoption Service - Basic workflow
6. ⬜ Notification Service - Email only (use SendGrid/MailKit)
7. ⬜ Enhance with preferences, advanced matching, etc.
