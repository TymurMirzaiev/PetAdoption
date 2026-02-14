# RabbitMQ Integration Guide

## Current Topology

### Exchanges
- **pet.events** (topic) - Pet domain events
- **pet.deadletter** (fanout) - Failed messages

### Queues
- **pet.reserved.notifications** → routing key: `pet.reserved.v1`
- **pet.adopted.notifications** → routing key: `pet.adopted.v1`
- **pet.reservation.cancelled.notifications** → routing key: `pet.reservation.cancelled.v1`

## Integration Patterns for Other Services

### Pattern 1: Subscribe to Existing Queues
**Use case:** Notification service that sends emails when pets are adopted

```csharp
// In Notification Service
var factory = new ConnectionFactory { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();

// Consume from existing queue
await channel.BasicConsumeAsync(
    queue: "pet.adopted.notifications",
    autoAck: false,
    consumer: new EventingBasicConsumer(channel)
);
```

### Pattern 2: Create Own Queue Bound to Existing Exchange
**Use case:** Analytics service that tracks all pet events

Add to the analytics service appsettings.json:
```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Queues": [
      {
        "Name": "pet.analytics.all-events",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "pet.events",
            "RoutingKey": "pet.*"  // Wildcard: all pet events
          }
        ]
      }
    ]
  }
}
```

### Pattern 3: Create Own Exchange and Queues
**Use case:** Adoption service with its own domain events

```json
{
  "RabbitMq": {
    "Exchanges": [
      {
        "Name": "adoption.events",
        "Type": "topic",
        "Durable": true
      }
    ],
    "Queues": [
      {
        "Name": "adoption.application.submitted",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "adoption.events",
            "RoutingKey": "adoption.application.submitted.v1"
          }
        ]
      }
    ]
  }
}
```

### Pattern 4: Cross-Service Communication
**Use case:** Adoption service reacts to pet events AND publishes its own

```json
{
  "RabbitMq": {
    "Exchanges": [
      {
        "Name": "adoption.events",
        "Type": "topic",
        "Durable": true
      }
    ],
    "Queues": [
      {
        "Name": "adoption.pet-adopted-handler",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "pet.events",
            "RoutingKey": "pet.adopted.v1"
          }
        ]
      },
      {
        "Name": "payment.adoption-completed",
        "Durable": true,
        "Bindings": [
          {
            "Exchange": "adoption.events",
            "RoutingKey": "adoption.completed.v1"
          }
        ]
      }
    ]
  }
}
```

## Routing Key Patterns

### Exact Match
```
"RoutingKey": "pet.adopted.v1"  // Only matches exactly
```

### Wildcard Patterns
```
"RoutingKey": "pet.*"           // Matches: pet.adopted.v1, pet.reserved.v1, etc.
"RoutingKey": "pet.#"           // Matches: pet.*, pet.adopted.v1.urgent, etc.
```

## Best Practices

1. **Versioning**: Always version your routing keys (`.v1`, `.v2`) for backward compatibility
2. **Namespacing**: Use hierarchical routing keys: `{domain}.{event}.{version}`
3. **Dead Letter Queues**: Configure DLX for all queues to handle failures
4. **Durability**: Use durable exchanges/queues for important events
5. **Idempotency**: Consumers should handle duplicate messages gracefully

## Reusing PetService Topology Setup

Other services can use the same configuration-based setup:

1. Copy `RabbitMqTopologyBuilder.cs` and `RabbitMqTopologySetup.cs` to your service
2. Add RabbitMQ configuration to appsettings.json
3. Register the hosted service:
   ```csharp
   builder.Services.AddHostedService<RabbitMqTopologySetup>();
   ```
4. The topology will auto-create on startup with conflict resolution
