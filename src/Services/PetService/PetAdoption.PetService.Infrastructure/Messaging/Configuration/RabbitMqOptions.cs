namespace PetAdoption.PetService.Infrastructure.Messaging.Configuration;

public class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public List<ExchangeConfig> Exchanges { get; set; } = new();
    public List<QueueConfig> Queues { get; set; } = new();
}

public class ExchangeConfig
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = "topic";
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; } = false;
}

public class QueueConfig
{
    public string Name { get; set; } = default!;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public List<BindingConfig> Bindings { get; set; } = new();
    public Dictionary<string, object>? Arguments { get; set; }
}

public class BindingConfig
{
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = string.Empty;
}
