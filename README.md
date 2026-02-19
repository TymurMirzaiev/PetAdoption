# PetAdoption

Pet project exploring microservices architecture.

**Key concepts:**
- Domain-Driven Design (DDD)
- Clean Architecture
- Microservices with RabbitMQ message broker
- Outbox pattern for reliable messaging
- MongoDB for persistence

## Project Structure

```
PetAdoption/
├── src/
│   ├── Services/
│   │   ├── PetService/              # ✅ Existing
│   │   ├── ChallengeService/        # ⬜ Planned
│   │   ├── DistributionService/     # ⬜ Planned
│   │   ├── AdoptionService/         # ⬜ Planned
│   │   ├── SchedulerService/        # ⬜ Planned
│   │   ├── NotificationService/     # ⬜ Planned
│   │   └── UserService/             # ⬜ Planned
│   └── Shared/
│       ├── PetAdoption.Shared.Messaging/    # Common event definitions
│       ├── PetAdoption.Shared.Domain/       # Shared value objects
│       └── PetAdoption.Shared.Infrastructure/ # Common infrastructure
├── docs/
│   ├── DEVELOPMENT-GUIDE.md
│   ├── adoption-challenge-feature-design.md
│   ├── adoption-challenge-architecture.md
│   └── rabbitmq-integration-example.md
└── compose.yaml                      # Docker Compose for all services
```

## Documentation

### Implementation Planning
- **[Implementation Plan](docs/IMPLEMENTATION-PLAN.md)** - Complete implementation roadmap with phases, tasks, and estimates
- **[Implementation Checklist](docs/IMPLEMENTATION-CHECKLIST.md)** - Quick reference checklist for tracking progress
- **[Service Dependencies](docs/SERVICE-DEPENDENCIES.md)** - Visual dependency graphs and integration patterns

### Architecture & Design
- [Development Guide](docs/DEVELOPMENT-GUIDE.md) - Implementation patterns and best practices
- [Adoption Challenge Feature Design](docs/adoption-challenge-feature-design.md) - Service breakdown and domain models
- [Architecture Overview](docs/adoption-challenge-architecture.md) - Service interactions and event flows
- [RabbitMQ Integration](docs/rabbitmq-integration-example.md) - Message broker topology and patterns

## Getting Started

### Quick Start
```bash
# 1. Clone the repository
git clone <repository-url>
cd PetAdoption

# 2. Start infrastructure
docker compose up mongo rabbitmq

# 3. Run PetService (currently implemented)
dotnet run --project src/Services/PetService/PetAdoption.PetService.API
```

### Implementation Order
Follow the dependency layers when implementing new services:
1. **Layer 0**: UserService (no dependencies)
2. **Layer 1**: ChallengeService (depends on UserService)
3. **Layer 2**: SchedulerService (depends on ChallengeService)
4. **Layer 3**: DistributionService (depends on ChallengeService, PetService, SchedulerService)
5. **Layer 4**: AdoptionService (depends on DistributionService)
6. **Layer 5**: NotificationService (depends on all services)

See [Implementation Plan](docs/IMPLEMENTATION-PLAN.md) for detailed steps.
