namespace PetAdoption.UserService.Infrastructure.BackgroundServices;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using RabbitMQ.Client;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private IConnection? _connection;
    private IChannel? _channel;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service starting...");

        // Wait a bit for RabbitMQ topology to be set up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Connect to RabbitMQ
        await ConnectToRabbitMq(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEvents(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            // Process every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ConnectToRabbitMq(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.Host,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.User,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ for outbox processing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    private async Task ProcessOutboxEvents(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var unprocessedEvents = await outboxRepository.GetUnprocessedAsync(batchSize: 100);

        if (unprocessedEvents.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox events", unprocessedEvents.Count);

        foreach (var outboxEvent in unprocessedEvents)
        {
            try
            {
                // Skip events that have been retried too many times
                if (outboxEvent.RetryCount >= 5)
                {
                    _logger.LogWarning(
                        "Skipping outbox event {EventId} - max retries exceeded",
                        outboxEvent.Id);
                    continue;
                }

                // Publish to RabbitMQ
                var body = Encoding.UTF8.GetBytes(outboxEvent.EventData);

                await _channel!.BasicPublishAsync(
                    exchange: "user.events",
                    routingKey: outboxEvent.RoutingKey,
                    body: body,
                    cancellationToken: cancellationToken);

                // Mark as processed
                await outboxRepository.MarkAsProcessedAsync(outboxEvent.Id);

                _logger.LogInformation(
                    "Published outbox event {EventId} ({EventType}) with routing key {RoutingKey}",
                    outboxEvent.Id,
                    outboxEvent.EventType,
                    outboxEvent.RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox event {EventId} ({EventType})",
                    outboxEvent.Id,
                    outboxEvent.EventType);

                await outboxRepository.MarkAsFailedAsync(outboxEvent.Id, ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Processor Service stopping...");

        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
