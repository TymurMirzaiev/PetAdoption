using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Infrastructure.Messaging.Configuration;

namespace PetAdoption.PetService.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        _logger.LogInformation("RabbitMQ Publisher initialized. Host: {Host}:{Port}",
            _factory.HostName, _factory.Port);
    }

    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        await EnsureConnection();
        await InternalPublish(domainEvent);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        await EnsureConnection();
        foreach (var domainEvent in domainEvents)
        {
            await InternalPublish(domainEvent);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
    
    private async Task EnsureConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _logger.LogInformation("Establishing RabbitMQ connection to {Host}:{Port}", _factory.HostName, _factory.Port);
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            _logger.LogInformation("RabbitMQ connection established");
        }
    }

    private async Task InternalPublish(IDomainEvent domainEvent)
    {
        var json = JsonSerializer.Serialize(domainEvent);
        var body = Encoding.UTF8.GetBytes(json);

        var exchange = RabbitMqTopology.Exchanges.PetEvents;
        var routingKey = GetRoutingKey(domainEvent);

        await _channel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body);

        _logger.LogInformation("Published {EventType} to exchange '{Exchange}' with routing key '{RoutingKey}'",
            domainEvent.GetType().Name, exchange, routingKey);
    }

    private static string GetRoutingKey(IDomainEvent domainEvent)
    {
        return domainEvent.GetType().Name switch
        {
            nameof(PetReservedEvent) => RabbitMqTopology.RoutingKeys.PetReserved,
            nameof(PetAdoptedEvent) => RabbitMqTopology.RoutingKeys.PetAdopted,
            nameof(PetReservationCancelledEvent) => RabbitMqTopology.RoutingKeys.ReservationCancelled,
            _ => string.Empty
        };
    }
}