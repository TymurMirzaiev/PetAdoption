using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly string _exchangeName;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration config)
    {
        var section = config.GetSection("RabbitMq");
        _exchangeName = section["Exchange"] ?? "pet_reservations";

        _factory = new ConnectionFactory
        {
            HostName = section["Host"] ?? "localhost",
            UserName = section["User"] ?? "guest",
            Password = section["Password"] ?? "guest",
        };
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
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName, 
                type: ExchangeType.Fanout, 
                durable: true);
        }
    }

    private async Task InternalPublish(IDomainEvent domainEvent)
    {
        var json = JsonSerializer.Serialize(domainEvent);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel!.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: string.Empty,
            body: body);
    }
}