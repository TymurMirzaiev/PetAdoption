# RabbitMQ Configuration Strategies

## Strategy 1: Configuration-Based Topology (Recommended for Simple Apps)

Define exchanges and queues in `appsettings.json` with declarative setup.

### Structure
```
Infrastructure/
├── Messaging/
│   ├── Configuration/
│   │   ├── RabbitMqConfiguration.cs
│   │   ├── ExchangeConfiguration.cs
│   │   └── QueueConfiguration.cs
│   ├── RabbitMqTopologySetup.cs
│   └── RabbitMqPublisher.cs
```

### Implementation

```csharp
// Infrastructure/Messaging/Configuration/RabbitMqConfiguration.cs
public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public List<ExchangeConfiguration> Exchanges { get; set; } = new();
    public List<QueueConfiguration> Queues { get; set; } = new();
}

public class ExchangeConfiguration
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = "fanout"; // fanout, direct, topic, headers
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; } = false;
}

public class QueueConfiguration
{
    public string Name { get; set; } = default!;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public List<BindingConfiguration> Bindings { get; set; } = new();
}

public class BindingConfiguration
{
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = "";
}

// appsettings.json
{
  "RabbitMq": {
    "Host": "localhost",
    "User": "guest",
    "Password": "guest",
    "Exchanges": [
      {
        "Name": "pet.events",
        "Type": "topic",
        "Durable": true
      },
      {
        "Name": "pet.commands",
        "Type": "direct",
        "Durable": true
      }
    ],
    "Queues": [
      {
        "Name": "pet.reserved.notifications",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "pet.events",
            "RoutingKey": "pet.reserved"
          }
        ]
      },
      {
        "Name": "pet.adopted.notifications",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "pet.events",
            "RoutingKey": "pet.adopted"
          }
        ]
      }
    ]
  }
}
```

---

## Strategy 2: Code-Based Topology Builder Pattern

Fluent API for defining topology in code with type safety.

### Implementation

```csharp
// Infrastructure/Messaging/RabbitMqTopologyBuilder.cs
public class RabbitMqTopologyBuilder
{
    private readonly List<Action<IChannel>> _setupActions = new();

    public RabbitMqTopologyBuilder DeclareExchange(
        string name,
        string type = "topic",
        bool durable = true,
        bool autoDelete = false)
    {
        _setupActions.Add(async channel =>
        {
            await channel.ExchangeDeclareAsync(name, type, durable, autoDelete);
        });
        return this;
    }

    public RabbitMqTopologyBuilder DeclareQueue(
        string name,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false)
    {
        _setupActions.Add(async channel =>
        {
            await channel.QueueDeclareAsync(name, durable, exclusive, autoDelete);
        });
        return this;
    }

    public RabbitMqTopologyBuilder BindQueue(
        string queue,
        string exchange,
        string routingKey = "")
    {
        _setupActions.Add(async channel =>
        {
            await channel.QueueBindAsync(queue, exchange, routingKey);
        });
        return this;
    }

    public async Task SetupAsync(IChannel channel)
    {
        foreach (var action in _setupActions)
        {
            await action(channel);
        }
    }
}

// Usage in Program.cs or Startup
var topology = new RabbitMqTopologyBuilder()
    .DeclareExchange("pet.events", "topic")
    .DeclareQueue("pet.reserved.notifications")
    .BindQueue("pet.reserved.notifications", "pet.events", "pet.reserved")
    .DeclareQueue("pet.adopted.notifications")
    .BindQueue("pet.adopted.notifications", "pet.events", "pet.adopted");

await topology.SetupAsync(channel);
```

---

## Strategy 3: Domain-Driven Exchange/Queue Organization

Organize by domain boundaries and event types.

### Naming Convention

```
Exchange Pattern: {domain}.{event-category}
Queue Pattern:    {domain}.{event-type}.{consumer}
Routing Key:      {domain}.{event-type}.{version}
```

### Example Structure

```csharp
public static class RabbitMqTopology
{
    // Exchanges
    public static class Exchanges
    {
        public const string PetEvents = "pet.events";
        public const string PetCommands = "pet.commands";
        public const string PetDeadLetter = "pet.deadletter";
    }

    // Queues
    public static class Queues
    {
        public static class Notifications
        {
            public const string PetReserved = "pet.reserved.notifications";
            public const string PetAdopted = "pet.adopted.notifications";
            public const string ReservationCancelled = "pet.reservation.cancelled.notifications";
        }

        public static class Analytics
        {
            public const string AllPetEvents = "pet.all.analytics";
        }

        public static class Integration
        {
            public const string ExternalSync = "pet.events.external-sync";
        }
    }

    // Routing Keys
    public static class RoutingKeys
    {
        public const string PetReserved = "pet.reserved.v1";
        public const string PetAdopted = "pet.adopted.v1";
        public const string ReservationCancelled = "pet.reservation.cancelled.v1";
    }
}
```

---

## Strategy 4: Event-to-Queue Mapper Pattern

Map domain events to RabbitMQ topology dynamically.

```csharp
// Infrastructure/Messaging/EventRoutingConfiguration.cs
public class EventRoutingConfiguration
{
    private readonly Dictionary<Type, RoutingConfig> _routes = new();

    public EventRoutingConfiguration()
    {
        // Configure routing for each event type
        Map<PetReservedEvent>(
            exchange: "pet.events",
            routingKey: "pet.reserved",
            queues: new[] { "notifications", "analytics" });

        Map<PetAdoptedEvent>(
            exchange: "pet.events",
            routingKey: "pet.adopted",
            queues: new[] { "notifications", "analytics", "external-sync" });
    }

    private void Map<TEvent>(string exchange, string routingKey, string[] queues)
    {
        _routes[typeof(TEvent)] = new RoutingConfig
        {
            Exchange = exchange,
            RoutingKey = routingKey,
            Queues = queues
        };
    }

    public RoutingConfig? GetRouting<TEvent>() =>
        _routes.GetValueOrDefault(typeof(TEvent));
}

// Modified RabbitMqPublisher
public async Task PublishAsync(IDomainEvent domainEvent)
{
    var routing = _routingConfig.GetRouting(domainEvent.GetType());
    if (routing == null) return;

    var json = JsonSerializer.Serialize(domainEvent);
    var body = Encoding.UTF8.GetBytes(json);

    await _channel.BasicPublishAsync(
        exchange: routing.Exchange,
        routingKey: routing.RoutingKey,
        body: body);
}
```

---

## Strategy 5: Multi-Tenancy Support

Separate topology per tenant or environment.

```csharp
// Infrastructure/Messaging/MultiTenantRabbitMqConfiguration.cs
public class MultiTenantRabbitMqConfiguration
{
    public Dictionary<string, TenantRabbitMqConfig> Tenants { get; set; } = new();
}

public class TenantRabbitMqConfig
{
    public string VirtualHost { get; set; } = "/";
    public string ExchangePrefix { get; set; } = "";
    public string QueuePrefix { get; set; } = "";
}

// appsettings.json
{
  "RabbitMq": {
    "Host": "localhost",
    "Tenants": {
      "tenant-a": {
        "VirtualHost": "/tenant-a",
        "ExchangePrefix": "a.",
        "QueuePrefix": "a."
      },
      "tenant-b": {
        "VirtualHost": "/tenant-b",
        "ExchangePrefix": "b.",
        "QueuePrefix": "b."
      }
    }
  }
}

// Usage
var tenantConfig = _multiTenantConfig.Tenants[tenantId];
var exchangeName = $"{tenantConfig.ExchangePrefix}pet.events";
```

---

## Strategy 6: Environment-Based Configuration

Different topology for different environments.

```csharp
// appsettings.Development.json
{
  "RabbitMq": {
    "Exchanges": [
      {
        "Name": "pet.events.dev",
        "Type": "fanout"  // Simple fanout for dev
      }
    ]
  }
}

// appsettings.Production.json
{
  "RabbitMq": {
    "Exchanges": [
      {
        "Name": "pet.events",
        "Type": "topic"  // Topic with routing for prod
      }
    ],
    "Queues": [
      {
        "Name": "pet.reserved.notifications",
        "Bindings": [
          { "Exchange": "pet.events", "RoutingKey": "pet.reserved" }
        ],
        "Arguments": {
          "x-dead-letter-exchange": "pet.deadletter",
          "x-message-ttl": 86400000
        }
      }
    ]
  }
}
```

---

## Recommended Approach for Your Project

### Phase 1: Simple Start (Current)
Use **Strategy 1** (Configuration-Based) for simplicity:

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "User": "guest",
    "Password": "guest",
    "Exchange": "pet.events",
    "ExchangeType": "topic"
  }
}
```

### Phase 2: Add Structure
Introduce **Strategy 3** (Domain-Driven) with constants:

```csharp
public static class RabbitMqTopology
{
    public const string PetEventsExchange = "pet.events";

    public static class RoutingKeys
    {
        public const string Reserved = "pet.reserved";
        public const string Adopted = "pet.adopted";
    }
}
```

### Phase 3: Scale
Add **Strategy 4** (Event Mapper) when you have multiple consumers.

---

## Best Practices

1. **Naming Conventions**
   - Use lowercase with dots: `pet.events.reserved`
   - Include version in routing keys: `pet.reserved.v1`
   - Prefix with domain: `petadoption.pet.events`

2. **Exchange Types**
   - **Topic**: Most flexible, use for events
   - **Direct**: Fast routing, use for commands
   - **Fanout**: Broadcast, use for pub/sub

3. **Queue Configuration**
   - Always set `Durable = true` for production
   - Use dead-letter exchanges for failed messages
   - Set TTL for temporary queues

4. **Error Handling**
   - Configure dead-letter exchanges
   - Set max retry counts
   - Log failed message routing

5. **Monitoring**
   - Expose queue depths as metrics
   - Track publish/consume rates
   - Alert on dead-letter queue growth
