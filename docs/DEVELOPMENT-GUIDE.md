# Development Guide - Microservices Architecture

## Project Structure

```
PetAdoption/
├── src/
│   ├── Services/
│   │   ├── PetService/              # ✅ Existing
│   │   ├── ChallengeService/        # ⬜ New
│   │   ├── DistributionService/     # ⬜ New
│   │   ├── AdoptionService/         # ⬜ New
│   │   ├── SchedulerService/        # ⬜ New
│   │   ├── NotificationService/     # ⬜ New
│   │   └── UserService/             # ⬜ New (if doesn't exist)
│   └── Shared/
│       ├── PetAdoption.Shared.Messaging/    # Common event definitions
│       ├── PetAdoption.Shared.Domain/       # Shared value objects
│       └── PetAdoption.Shared.Infrastructure/ # Common infrastructure
├── docs/
└── compose.yaml                      # Docker Compose for all services
```

## Service Template Structure (Clean Architecture)

Each service follows the same structure as PetService:

```
ServiceName/
├── ServiceName.API/                  # Web API layer
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── ServiceName.Application/          # Application logic
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
├── ServiceName.Domain/               # Domain models
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── Repositories/
└── ServiceName.Infrastructure/       # Data access, messaging
    ├── Persistence/
    ├── Messaging/
    └── Configuration/
```

## Development Workflow

### 1. Creating a New Service

**Step 1: Create Project Structure**
```bash
# From solution root
dotnet new sln -n PetAdoption.ChallengeService -o src/Services/ChallengeService
cd src/Services/ChallengeService

# Create projects
dotnet new webapi -n PetAdoption.ChallengeService.API
dotnet new classlib -n PetAdoption.ChallengeService.Application
dotnet new classlib -n PetAdoption.ChallengeService.Domain
dotnet new classlib -n PetAdoption.ChallengeService.Infrastructure

# Add to solution
dotnet sln add **/*.csproj

# Add project references
dotnet add API reference Application Infrastructure
dotnet add Application reference Domain
dotnet add Infrastructure reference Domain
```

**Step 2: Copy Common Infrastructure**

Copy from PetService:
- `RabbitMqTopologyBuilder.cs`
- `RabbitMqTopologySetup.cs`
- `RabbitMqOptions.cs` and related config classes
- `MongoDbContext` patterns (if using MongoDB)

**Step 3: Configure appsettings.json**

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://root:example@localhost:27017"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Exchanges": [
      {
        "Name": "challenge.events",
        "Type": "topic",
        "Durable": true
      }
    ],
    "Queues": []
  }
}
```

**Step 4: Setup Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

// MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("MongoDB connection string is not configured");
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("ChallengeDb");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// RabbitMQ Configuration
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<RabbitMqTopologySetup>();

// Repositories
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();

// Application Services
builder.Services.AddScoped<ICommandHandler<CreateChallenge>, CreateChallengeHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

### 2. Domain Modeling Pattern

**Entity Example:**
```csharp
namespace PetAdoption.ChallengeService.Domain.Entities;

public class AdoptionChallenge : Entity
{
    public ChallengeId Id { get; private set; }
    public string Title { get; private set; }
    public DateTime DistributionDateTime { get; private set; }
    public ChallengeStatus Status { get; private set; }

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static AdoptionChallenge Create(string title, DateTime distributionDateTime)
    {
        var challenge = new AdoptionChallenge
        {
            Id = ChallengeId.Create(),
            Title = title,
            DistributionDateTime = distributionDateTime,
            Status = ChallengeStatus.Draft
        };

        challenge.AddDomainEvent(new ChallengeCreatedEvent(challenge.Id));
        return challenge;
    }

    public void Open()
    {
        if (Status != ChallengeStatus.Draft)
            throw new InvalidOperationException("Only draft challenges can be opened");

        Status = ChallengeStatus.Open;
        AddDomainEvent(new ChallengeOpenedEvent(Id));
    }

    private void AddDomainEvent(DomainEvent @event)
    {
        _domainEvents.Add(@event);
    }
}
```

**Value Object Example:**
```csharp
public record ChallengeId
{
    public string Value { get; init; }

    private ChallengeId(string value) => Value = value;

    public static ChallengeId Create() => new(Guid.NewGuid().ToString());
    public static ChallengeId From(string value) => new(value);
}
```

### 3. Event Publishing Pattern

**Domain Event:**
```csharp
namespace PetAdoption.ChallengeService.Domain.Events;

public record ChallengeCreatedEvent(ChallengeId ChallengeId) : DomainEvent;
```

**Event Publisher (Infrastructure):**
```csharp
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public async Task PublishAsync<T>(T @event, string routingKey) where T : DomainEvent
    {
        var channel = await _connection.CreateChannelAsync();

        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "challenge.events",
            routingKey: routingKey,
            body: body);

        _logger.LogInformation(
            "Published event {EventType} with routing key {RoutingKey}",
            typeof(T).Name,
            routingKey);
    }
}
```

**Using in Repository:**
```csharp
public class ChallengeRepository : IChallengeRepository
{
    private readonly IMongoCollection<AdoptionChallenge> _collection;
    private readonly IEventPublisher _eventPublisher;

    public async Task SaveAsync(AdoptionChallenge challenge)
    {
        await _collection.ReplaceOneAsync(
            c => c.Id == challenge.Id,
            challenge,
            new ReplaceOptions { IsUpsert = true });

        // Publish domain events
        foreach (var @event in challenge.DomainEvents)
        {
            await _eventPublisher.PublishAsync(@event, GetRoutingKey(@event));
        }
    }

    private string GetRoutingKey(DomainEvent @event) => @event switch
    {
        ChallengeCreatedEvent => "challenge.created.v1",
        ChallengeOpenedEvent => "challenge.opened.v1",
        _ => throw new ArgumentException($"Unknown event type {@event.GetType()}")
    };
}
```

### 4. Event Consumption Pattern

**Background Service Consumer:**
```csharp
public class ChallengeEventConsumer : BackgroundService
{
    private readonly ILogger<ChallengeEventConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received: {Message}", message);

            // Process message
            await ProcessEventAsync(ea.RoutingKey, message);

            // Acknowledge
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: "distribution.challenge-ready-handler",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessEventAsync(string routingKey, string message)
    {
        switch (routingKey)
        {
            case "challenge.ready-for-distribution.v1":
                var @event = JsonSerializer.Deserialize<ChallengeReadyEvent>(message);
                // Handle event
                break;
        }
    }
}
```

### 5. Docker Compose Integration

Add new service to `compose.yaml`:

```yaml
services:
  petadoption.challengeservice:
    image: petadoption.challengeservice
    build:
      context: .
      dockerfile: src/Services/ChallengeService/PetAdoption.ChallengeService.API/Dockerfile
    ports:
      - "8081:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      mongo:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
```

### 6. Implementing Outbox Pattern (Recommended)

For reliable event publishing, use the Outbox pattern (already in PetService):

**OutboxMessage Entity:**
```csharp
public class OutboxMessage
{
    public string Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public string RoutingKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
}
```

**Save to Outbox instead of immediate publish:**
```csharp
public async Task SaveAsync(AdoptionChallenge challenge)
{
    // Save entity and outbox messages in same transaction/operation
    await _collection.ReplaceOneAsync(...);

    foreach (var @event in challenge.DomainEvents)
    {
        var outboxMessage = new OutboxMessage
        {
            EventType = @event.GetType().Name,
            Payload = JsonSerializer.Serialize(@event),
            RoutingKey = GetRoutingKey(@event),
            CreatedAt = DateTime.UtcNow
        };

        await _outboxCollection.InsertOneAsync(outboxMessage);
    }
}
```

**Background processor publishes from outbox:**
```csharp
public class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var unprocessed = await _outbox
                .Find(m => !m.IsProcessed)
                .Limit(100)
                .ToListAsync();

            foreach (var message in unprocessed)
            {
                await _publisher.PublishAsync(message.Payload, message.RoutingKey);

                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                await _outbox.ReplaceOneAsync(m => m.Id == message.Id, message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

## Best Practices

### 1. Repository Pattern
```csharp
public interface IChallengeRepository
{
    Task<AdoptionChallenge?> GetByIdAsync(ChallengeId id);
    Task<List<AdoptionChallenge>> GetActiveAsync();
    Task SaveAsync(AdoptionChallenge challenge);
    Task DeleteAsync(ChallengeId id);
}
```

### 2. Command/Query Separation (CQRS Light)

**Command:**
```csharp
public record CreateChallengeCommand(
    string Title,
    DateTime DistributionDateTime,
    int MaxParticipants);

public class CreateChallengeHandler : ICommandHandler<CreateChallengeCommand>
{
    public async Task<ChallengeId> HandleAsync(CreateChallengeCommand command)
    {
        var challenge = AdoptionChallenge.Create(
            command.Title,
            command.DistributionDateTime);

        await _repository.SaveAsync(challenge);
        return challenge.Id;
    }
}
```

**Query:**
```csharp
public record GetActiveChallengesQuery;

public class GetActiveChallengesHandler : IQueryHandler<GetActiveChallengesQuery>
{
    public async Task<List<ChallengeDto>> HandleAsync(GetActiveChallengesQuery query)
    {
        var challenges = await _repository.GetActiveAsync();
        return challenges.Select(c => new ChallengeDto
        {
            Id = c.Id.Value,
            Title = c.Title,
            // ...
        }).ToList();
    }
}
```

### 3. API Controller Pattern
```csharp
[ApiController]
[Route("api/challenges")]
public class ChallengesController : ControllerBase
{
    private readonly ICommandHandler<CreateChallengeCommand> _createHandler;
    private readonly IQueryHandler<GetActiveChallengesQuery> _queryHandler;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChallengeRequest request)
    {
        var command = new CreateChallengeCommand(
            request.Title,
            request.DistributionDateTime,
            request.MaxParticipants);

        var challengeId = await _createHandler.HandleAsync(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id = challengeId.Value },
            new { id = challengeId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var query = new GetActiveChallengesQuery();
        var challenges = await _queryHandler.HandleAsync(query);
        return Ok(challenges);
    }
}
```

### 4. Error Handling

**Custom Exceptions:**
```csharp
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class ChallengeNotFoundException : DomainException
{
    public ChallengeNotFoundException(ChallengeId id)
        : base("CHALLENGE_NOT_FOUND", $"Challenge {id.Value} not found")
    {
    }
}
```

**Global Exception Handler:**
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (statusCode, code, message) = exception switch
        {
            DomainException de => (400, de.Code, de.Message),
            _ => (500, "INTERNAL_ERROR", "An error occurred")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            code,
            message
        });
    });
});
```

### 5. Testing Strategy

**Unit Tests (Domain):**
```csharp
public class AdoptionChallengeTests
{
    [Fact]
    public void Create_ShouldRaiseChallengeCreatedEvent()
    {
        // Arrange & Act
        var challenge = AdoptionChallenge.Create("Test", DateTime.UtcNow.AddDays(7));

        // Assert
        challenge.DomainEvents.Should().ContainSingle(e => e is ChallengeCreatedEvent);
    }
}
```

**Integration Tests (API):**
```csharp
public class ChallengesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { title = "Test", distributionDateTime = "2026-04-01T10:00:00Z" };

        // Act
        var response = await client.PostAsJsonAsync("/api/challenges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## Development Order Recommendation

1. ✅ **PetService** (existing)
2. **ChallengeService** - Foundation for the feature
3. **SchedulerService** - Simple at first (Timer-based)
4. **DistributionService** - Core matching logic
5. **AdoptionService** - Application workflow
6. **NotificationService** - Cross-cutting concern
7. **UserService** - If doesn't exist

## Common Pitfalls to Avoid

❌ **Don't:** Create circular dependencies between services
✅ **Do:** Use events for cross-service communication

❌ **Don't:** Share databases between services
✅ **Do:** Each service has its own database

❌ **Don't:** Make synchronous HTTP calls between services for critical flows
✅ **Do:** Use async messaging (events) for service-to-service communication

❌ **Don't:** Put business logic in controllers
✅ **Do:** Keep controllers thin, logic in domain/application layer

❌ **Don't:** Forget to version your events
✅ **Do:** Always include version in routing keys (`.v1`, `.v2`)

❌ **Don't:** Auto-acknowledge RabbitMQ messages before processing
✅ **Do:** Process first, then acknowledge (at-least-once delivery)

## Monitoring & Observability

**Add to each service:**

1. **Health Checks:**
```csharp
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString)
    .AddRabbitMQ(rabbitConnectionString);

app.MapHealthChecks("/health");
```

2. **Structured Logging:**
```csharp
Log.Information(
    "Challenge created: {ChallengeId} at {DistributionTime}",
    challenge.Id,
    challenge.DistributionDateTime);
```

3. **Metrics:**
```csharp
// Track important business metrics
_metrics.IncrementCounter("challenges_created_total");
_metrics.RecordHistogram("distribution_duration_seconds", duration);
```

## Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [MongoDB with .NET](https://www.mongodb.com/docs/drivers/csharp/)
