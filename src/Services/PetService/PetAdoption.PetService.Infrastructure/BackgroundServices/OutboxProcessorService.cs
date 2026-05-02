using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetryCount = OutboxEvent.MaxRetryCount;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private static readonly Assembly _domainAssembly = typeof(IDomainEvent).Assembly;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEvents(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Service stopped");
    }

    private async Task ProcessPendingEvents(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pendingEvents = await outboxRepository.GetPendingEventsAsync(batchSize: BatchSize);
        var eventsList = pendingEvents.ToList();

        if (!eventsList.Any())
            return;

        _logger.LogInformation("Processing {EventCount} pending outbox events", eventsList.Count);

        foreach (var outboxEvent in eventsList)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // Deserialize and publish the event
                var domainEvent = DeserializeEvent(outboxEvent);
                if (domainEvent != null)
                {
                    await eventPublisher.PublishAsync(domainEvent);

                    // Mark as processed
                    outboxEvent.MarkAsProcessed();
                    await outboxRepository.UpdateAsync(outboxEvent);

                    _logger.LogDebug(
                        "Successfully published outbox event {EventId} of type {EventType}",
                        outboxEvent.Id,
                        outboxEvent.EventType);
                }
            }
            catch (Exception ex)
            {
                outboxEvent.RecordFailure(ex.Message);

                if (outboxEvent.RetryCount >= MaxRetryCount)
                {
                    _logger.LogError(
                        ex,
                        "Outbox event {EventId} of type {EventType} has permanently failed after {MaxRetries} retries. It will no longer be retried.",
                        outboxEvent.Id,
                        outboxEvent.EventType,
                        MaxRetryCount);
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to publish outbox event {EventId} of type {EventType}. Retry count: {RetryCount}/{MaxRetries}",
                        outboxEvent.Id,
                        outboxEvent.EventType,
                        outboxEvent.RetryCount,
                        MaxRetryCount);
                }

                await outboxRepository.UpdateAsync(outboxEvent);
            }
        }

        _logger.LogInformation("Completed processing {EventCount} outbox events", eventsList.Count);
    }

    private IDomainEvent? DeserializeEvent(OutboxEvent outboxEvent)
    {
        try
        {
            var eventType = _domainAssembly.GetType($"PetAdoption.PetService.Domain.{outboxEvent.EventType}");
            if (eventType == null)
            {
                _logger.LogWarning("Could not find event type {EventType}", outboxEvent.EventType);
                return null;
            }

            var domainEvent = JsonSerializer.Deserialize(outboxEvent.EventData, eventType) as IDomainEvent;
            return domainEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing event {EventId}", outboxEvent.Id);
            return null;
        }
    }
}
