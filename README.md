# PetAdoption

A .NET 10 microservices platform for pet adoption implementing Clean Architecture, DDD, CQRS, and event-driven patterns.

## 🎉 Latest Achievement

**UserService is now complete and production-ready!**
- ✅ 53/53 E2E tests passed (100% success rate)
- ✅ JWT authentication & role-based authorization
- ✅ MongoDB persistence with value object serialization
- ✅ RabbitMQ event publishing with Transactional Outbox Pattern
- ✅ Docker Compose ready with full documentation

📄 [View E2E Test Results](src/Services/UserService/E2E-VERIFICATION-COMPLETE.md)

**Key concepts:**
- Domain-Driven Design (DDD) with tactical patterns
- Clean Architecture with strict dependency rules
- CQRS (Command Query Responsibility Segregation)
- Event-driven architecture with RabbitMQ
- Transactional Outbox Pattern for reliable messaging
- MongoDB 8.0 for persistence
- JWT authentication & BCrypt password hashing
- Docker containerization

## Project Structure

```
PetAdoption/
├── src/
│   ├── Services/
│   │   ├── UserService/             # ✅ Complete (E2E verified 100%)
│   │   ├── PetService/              # 🚧 In Progress
│   │   ├── ChallengeService/        # ⬜ Planned
│   │   ├── DistributionService/     # ⬜ Planned
│   │   ├── AdoptionService/         # ⬜ Planned
│   │   ├── SchedulerService/        # ⬜ Planned
│   │   └── NotificationService/     # ⬜ Planned
│   └── Shared/
│       ├── PetAdoption.Shared.Messaging/    # Common event definitions
│       ├── PetAdoption.Shared.Domain/       # Shared value objects
│       └── PetAdoption.Shared.Infrastructure/ # Common infrastructure
├── docs/
│   ├── CLAUDE.md                     # ✅ Updated with UserService patterns
│   ├── DEVELOPMENT-GUIDE.md
│   ├── adoption-challenge-feature-design.md
│   ├── adoption-challenge-architecture.md
│   └── rabbitmq-integration-example.md
└── compose.yaml                      # Docker Compose for all services
```

## Service Status

| Service | Status | Features | E2E Tests | Documentation |
|---------|--------|----------|-----------|---------------|
| **UserService** | ✅ **Complete** | Authentication, Authorization, User Management | 53/53 (100%) | [README](src/Services/UserService/README.md), [E2E Results](src/Services/UserService/E2E-VERIFICATION-COMPLETE.md) |
| **PetService** | 🚧 In Progress | Pet Management, Reservations | Not Started | [CLAUDE.md](docs/CLAUDE.md) |
| **ChallengeService** | ⬜ Planned | Adoption Challenges | Not Started | [Architecture](docs/adoption-challenge-architecture.md) |
| **DistributionService** | ⬜ Planned | Pet Distribution | Not Started | - |
| **AdoptionService** | ⬜ Planned | Adoption Processing | Not Started | - |
| **SchedulerService** | ⬜ Planned | Challenge Scheduling | Not Started | - |
| **NotificationService** | ⬜ Planned | Notifications | Not Started | - |

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

### Quick Start - UserService (Fully Functional)

**Option 1: Docker (Recommended)**
```bash
# Start UserService with MongoDB and RabbitMQ
cd src/Services/UserService
docker-compose up -d --build

# Test registration
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","fullName":"Test User","password":"SecurePass123!","phoneNumber":"+1234567890"}'

# Test login
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"SecurePass123!"}'

# View logs
docker logs petadoption-userservice -f
```

**Option 2: Local .NET**
```bash
# 1. Start infrastructure
docker compose up mongo rabbitmq

# 2. Run UserService
dotnet run --project src/Services/UserService/PetAdoption.UserService.API
```

### PetService (In Progress)
```bash
# 1. Start infrastructure
docker compose up mongo rabbitmq

# 2. Run PetService
dotnet run --project src/Services/PetService/PetAdoption.PetService.API
```

### Implementation Order
Follow the dependency layers when implementing new services:
1. **Layer 0**: ✅ **UserService (COMPLETE)** - Authentication & user management (no dependencies)
2. **Layer 1**: ChallengeService (depends on UserService)
3. **Layer 2**: SchedulerService (depends on ChallengeService)
4. **Layer 3**: DistributionService (depends on ChallengeService, PetService, SchedulerService)
5. **Layer 4**: AdoptionService (depends on DistributionService)
6. **Layer 5**: NotificationService (depends on all services)

**Next Step**: Complete PetService implementation, then integrate with UserService for adoptions.

See [CLAUDE.md](docs/CLAUDE.md) for detailed development patterns and [Implementation Plan](docs/IMPLEMENTATION-PLAN.md) for complete roadmap.
