# Random Pet Adoption Challenge - Feature Design

## Overview
Users register for a challenge, then at a scheduled time, pets are randomly assigned to participants and the adoption process begins.

## Required Services

### 1. Challenge Service (NEW)
**Responsibility:** Manage adoption challenge lifecycle

**Domain Model:**
```csharp
public class AdoptionChallenge
{
    public ChallengeId Id { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime DistributionDateTime { get; }
    public ChallengeStatus Status { get; } // Draft, Open, InProgress, Completed, Cancelled
    public int MaxParticipants { get; }
    public int MinPetsRequired { get; }
    public List<ParticipantId> Participants { get; }
    public List<PetId> AvailablePets { get; }
}

public class Participant
{
    public ParticipantId Id { get; }
    public UserId UserId { get; }
    public ChallengeId ChallengeId { get; }
    public DateTime RegisteredAt { get; }
    public UserPreferences Preferences { get; } // e.g., dog/cat, size, age
}
```

**API Endpoints:**
- `POST /api/challenges` - Create challenge (admin)
- `GET /api/challenges` - List active challenges
- `POST /api/challenges/{id}/register` - Register for challenge
- `DELETE /api/challenges/{id}/register` - Unregister
- `GET /api/challenges/{id}/participants` - List participants

**Domain Events:**
- `challenge.created.v1`
- `challenge.opened.v1`
- `challenge.participant-registered.v1`
- `challenge.participant-unregistered.v1`
- `challenge.ready-for-distribution.v1`
- `challenge.cancelled.v1`

**Database:** MongoDB
- Collections: Challenges, Participants

---

### 2. Distribution Service (NEW)
**Responsibility:** Match pets with participants when challenge starts

**Domain Model:**
```csharp
public class Distribution
{
    public DistributionId Id { get; }
    public ChallengeId ChallengeId { get; }
    public DateTime ScheduledAt { get; }
    public DateTime? ExecutedAt { get; }
    public DistributionStatus Status { get; } // Scheduled, Running, Completed, Failed
    public List<Match> Matches { get; }
}

public class Match
{
    public MatchId Id { get; }
    public PetId PetId { get; }
    public ParticipantId ParticipantId { get; }
    public int MatchScore { get; } // How well they matched
}
```

**Matching Algorithm (can be simple random or preference-based):**
```csharp
public interface IMatchingStrategy
{
    List<Match> CreateMatches(List<Pet> pets, List<Participant> participants);
}

// Simple random
public class RandomMatchingStrategy : IMatchingStrategy { }

// Preference-based (future enhancement)
public class PreferenceBasedMatchingStrategy : IMatchingStrategy { }
```

**Domain Events:**
- `distribution.started.v1`
- `distribution.match-created.v1`
- `distribution.completed.v1`
- `distribution.failed.v1`

**Consumes Events:**
- `challenge.ready-for-distribution.v1`

**Database:** MongoDB
- Collections: Distributions, Matches

---

### 3. Scheduler Service (NEW)
**Responsibility:** Trigger time-based events

**Implementation Options:**
- **Hangfire** - Background job scheduler
- **Quartz.NET** - Enterprise scheduler
- **Simple Timer** - Custom implementation with Timer/PeriodicTimer

**Functionality:**
- Monitor upcoming challenge distribution times
- Publish `challenge.ready-for-distribution.v1` at scheduled time
- Handle recurring challenges (future)

**Consumes Events:**
- `challenge.created.v1` - Schedule distribution
- `challenge.cancelled.v1` - Cancel scheduled job

**Database:** SQL Server or PostgreSQL (for Hangfire/Quartz persistence)

---

### 4. Adoption Service (NEW or extend Pet Service)
**Responsibility:** Manage adoption process lifecycle

**Domain Model:**
```csharp
public class AdoptionApplication
{
    public ApplicationId Id { get; }
    public PetId PetId { get; }
    public UserId UserId { get; }
    public ApplicationSource Source { get; } // Challenge, Direct
    public MatchId? ChallengeMatchId { get; }
    public ApplicationStatus Status { get; } // Initiated, UnderReview, Approved, Rejected, Completed
    public DateTime CreatedAt { get; }
    public DateTime? CompletedAt { get; }
}
```

**API Endpoints:**
- `POST /api/adoptions/applications` - Start adoption (manual or from challenge)
- `GET /api/adoptions/applications/{id}` - Get application status
- `PUT /api/adoptions/applications/{id}/approve` - Approve
- `PUT /api/adoptions/applications/{id}/reject` - Reject
- `PUT /api/adoptions/applications/{id}/complete` - Mark as completed

**Domain Events:**
- `adoption.application-created.v1`
- `adoption.application-approved.v1`
- `adoption.application-rejected.v1`
- `adoption.application-completed.v1`

**Consumes Events:**
- `distribution.match-created.v1` - Auto-create adoption application

**Database:** MongoDB
- Collections: AdoptionApplications

---

### 5. Notification Service (NEW)
**Responsibility:** Send notifications to users

**Notification Types:**
- Email
- Push notifications
- SMS (future)
- In-app notifications

**Templates:**
- Challenge registration confirmation
- Challenge starting soon (24h, 1h before)
- Pet assigned notification
- Adoption application status updates

**Consumes Events:**
- `challenge.participant-registered.v1`
- `distribution.match-created.v1`
- `adoption.application-created.v1`
- `adoption.application-approved.v1`
- etc.

**Database:** MongoDB (for notification history)

---

### 6. Pet Service (EXISTING - Enhanced)
**Changes Needed:**
- Add pet availability status for challenges
- Support reserving pets for challenges

**New Domain Events:**
- `pet.reserved-for-challenge.v1`
- `pet.released-from-challenge.v1`

---

### 7. User Service (May already exist)
**Responsibility:** User authentication, profiles, preferences

**If doesn't exist, minimal implementation needed:**
- User registration/login
- User profile management
- User preferences for pet matching

---

## Event Flow

### Registration Flow
```
1. User → Challenge Service: Register for challenge
2. Challenge Service → RabbitMQ: challenge.participant-registered.v1
3. Notification Service ← RabbitMQ: Send confirmation email
```

### Distribution Flow
```
1. Scheduler Service: Detects it's distribution time
2. Scheduler → RabbitMQ: challenge.ready-for-distribution.v1
3. Distribution Service ← RabbitMQ: Start matching algorithm
4. Distribution Service → Challenge Service: Get participants & pets
5. Distribution Service: Run matching algorithm
6. Distribution Service → RabbitMQ: distribution.match-created.v1 (for each match)
7. Adoption Service ← RabbitMQ: Create adoption application
8. Notification Service ← RabbitMQ: Send "You got matched!" notification
9. Distribution Service → RabbitMQ: distribution.completed.v1
```

### Adoption Flow
```
1. Adoption Service: Application created (from match)
2. Adoption Service → RabbitMQ: adoption.application-created.v1
3. Notification Service ← RabbitMQ: Notify user and admin
4. Admin approves application
5. Adoption Service → RabbitMQ: adoption.application-approved.v1
6. Pet Service ← RabbitMQ: Mark pet as adopted
7. Notification Service ← RabbitMQ: Send success notification
```

## RabbitMQ Topology

### New Exchanges
```json
{
  "Exchanges": [
    {
      "Name": "challenge.events",
      "Type": "topic",
      "Durable": true
    },
    {
      "Name": "distribution.events",
      "Type": "topic",
      "Durable": true
    },
    {
      "Name": "adoption.events",
      "Type": "topic",
      "Durable": true
    }
  ]
}
```

### Example Queues
```json
{
  "Queues": [
    {
      "Name": "distribution.challenge-ready-handler",
      "Bindings": [
        {
          "Exchange": "challenge.events",
          "RoutingKey": "challenge.ready-for-distribution.v1"
        }
      ]
    },
    {
      "Name": "adoption.match-created-handler",
      "Bindings": [
        {
          "Exchange": "distribution.events",
          "RoutingKey": "distribution.match-created.v1"
        }
      ]
    },
    {
      "Name": "notification.all-events",
      "Bindings": [
        {
          "Exchange": "challenge.events",
          "RoutingKey": "challenge.#"
        },
        {
          "Exchange": "distribution.events",
          "RoutingKey": "distribution.#"
        },
        {
          "Exchange": "adoption.events",
          "RoutingKey": "adoption.#"
        }
      ]
    }
  ]
}
```

## Technology Stack per Service

| Service | API | Database | Message Broker |
|---------|-----|----------|----------------|
| Challenge | ASP.NET Core | MongoDB | RabbitMQ |
| Distribution | ASP.NET Core | MongoDB | RabbitMQ |
| Scheduler | Background Worker | SQL/Hangfire | RabbitMQ |
| Adoption | ASP.NET Core | MongoDB | RabbitMQ |
| Notification | Background Worker | MongoDB | RabbitMQ |
| Pet | ASP.NET Core (existing) | MongoDB | RabbitMQ |

## Implementation Phases

### Phase 1: MVP (Minimum Viable Product)
- Challenge Service (basic CRUD)
- Distribution Service (random matching)
- Adoption Service (basic application flow)
- Simple Scheduler (Timer-based)
- Basic email notifications

### Phase 2: Enhanced
- Notification Service (proper service with multiple channels)
- Preference-based matching
- Challenge templates
- Recurring challenges

### Phase 3: Advanced
- Machine learning for better matching
- User rating system
- Challenge analytics
- Mobile app integration

## Database Schema Examples

### Challenge Service - MongoDB
```javascript
// challenges collection
{
  "_id": "challenge-123",
  "title": "Weekend Adoption Challenge",
  "description": "Help pets find homes this weekend!",
  "distributionDateTime": ISODate("2026-03-15T10:00:00Z"),
  "status": "Open",
  "maxParticipants": 50,
  "minPetsRequired": 20,
  "createdAt": ISODate("2026-03-01T10:00:00Z"),
  "updatedAt": ISODate("2026-03-10T15:30:00Z")
}

// participants collection
{
  "_id": "participant-456",
  "userId": "user-789",
  "challengeId": "challenge-123",
  "registeredAt": ISODate("2026-03-05T14:20:00Z"),
  "preferences": {
    "petType": "Dog",
    "size": ["Small", "Medium"],
    "ageRange": "Young"
  }
}
```

### Distribution Service - MongoDB
```javascript
// distributions collection
{
  "_id": "distribution-abc",
  "challengeId": "challenge-123",
  "scheduledAt": ISODate("2026-03-15T10:00:00Z"),
  "executedAt": ISODate("2026-03-15T10:00:03Z"),
  "status": "Completed",
  "matches": [
    {
      "matchId": "match-1",
      "petId": "pet-001",
      "participantId": "participant-456",
      "matchScore": 85
    }
  ]
}
```

## API Example

### Create Challenge
```http
POST /api/challenges
Content-Type: application/json

{
  "title": "Spring Adoption Challenge",
  "description": "Find your new best friend!",
  "distributionDateTime": "2026-04-01T10:00:00Z",
  "maxParticipants": 100,
  "minPetsRequired": 50
}
```

### Register for Challenge
```http
POST /api/challenges/challenge-123/register
Content-Type: application/json

{
  "userId": "user-789",
  "preferences": {
    "petType": "Dog",
    "size": ["Small", "Medium"]
  }
}
```

## Security Considerations

1. **Challenge Service**: Only admins can create challenges
2. **Distribution Service**: Internal service, no public API
3. **Adoption Service**: Users can only see their own applications
4. **Rate Limiting**: Prevent spam registrations
5. **Idempotency**: Handle duplicate event processing

## Observability

- **Logging**: Structured logging with Serilog
- **Metrics**: Track challenge registrations, match success rate, adoption completion rate
- **Tracing**: Distributed tracing with OpenTelemetry
- **Health Checks**: Each service exposes /health endpoint
