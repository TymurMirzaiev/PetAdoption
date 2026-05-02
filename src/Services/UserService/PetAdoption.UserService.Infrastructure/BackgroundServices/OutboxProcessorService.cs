namespace PetAdoption.UserService.Infrastructure.BackgroundServices;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Infrastructure.Messaging;
using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using RabbitMQ.Client;

public class OutboxProcessorService : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ConnectionFactory _connectionFactory;
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

        _connectionFactory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.Host,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.User,
            Password = _rabbitMqOptions.Password,
            VirtualHost = _rabbitMqOptions.VirtualHost
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service starting...");

        // Wait a bit for RabbitMQ topology to be set up
        await Task.Delay(PollingInterval, stoppingToken);

        // Connect to RabbitMQ
        await EnsureConnection(stoppingToken);

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

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task EnsureConnection(CancellationToken cancellationToken = default)
    {
        if (_connection?.IsOpen == true)
            return;

        try
        {
            _logger.LogInformation("Establishing RabbitMQ connection to {Host}:{Port}",
                _connectionFactory.HostName, _connectionFactory.Port);

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
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
        await EnsureConnection(cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var unprocessedEvents = await outboxRepository.GetUnprocessedAsync(batchSize: BatchSize);

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
                if (outboxEvent.RetryCount >= MaxRetryCount)
                {
                    _logger.LogWarning(
                        "Skipping outbox event {EventId} - max retries exceeded",
                        outboxEvent.Id);
                    continue;
                }

                // Publish to RabbitMQ
                var body = Encoding.UTF8.GetBytes(outboxEvent.EventData);

                await _channel!.BasicPublishAsync(
                    exchange: UserRabbitMqTopology.Exchanges.UserEvents,
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
