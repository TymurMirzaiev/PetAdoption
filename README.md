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

- [Development Guide](docs/DEVELOPMENT-GUIDE.md) - Implementation patterns and best practices
- [Adoption Challenge Feature Design](docs/adoption-challenge-feature-design.md) - Service breakdown and domain models
- [Architecture Overview](docs/adoption-challenge-architecture.md) - Service interactions and event flows
- [RabbitMQ Integration](docs/rabbitmq-integration-example.md) - Message broker topology and patterns
