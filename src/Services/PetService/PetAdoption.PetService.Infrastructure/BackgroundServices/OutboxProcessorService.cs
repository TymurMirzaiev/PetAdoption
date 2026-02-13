using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes pending events from the outbox and publishes them to RabbitMQ.
/// Runs periodically to ensure eventual consistency and reliable event delivery.
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

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

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Service stopped");
    }

    private async Task ProcessPendingEvents(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pendingEvents = await outboxRepository.GetPendingEvents(batchSize: 100);
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
                    await outboxRepository.Update(outboxEvent);

                    _logger.LogDebug(
                        "Successfully published outbox event {EventId} of type {EventType}",
                        outboxEvent.Id,
                        outboxEvent.EventType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish outbox event {EventId} of type {EventType}. Retry count: {RetryCount}",
                    outboxEvent.Id,
                    outboxEvent.EventType,
                    outboxEvent.RetryCount);

                // Record failure
                outboxEvent.RecordFailure(ex.Message);
                await outboxRepository.Update(outboxEvent);
            }
        }

        _logger.LogInformation("Completed processing {EventCount} outbox events", eventsList.Count);
    }

    private IDomainEvent? DeserializeEvent(OutboxEvent outboxEvent)
    {
        try
        {
            // Get the event type
            var eventType = Type.GetType($"PetAdoption.PetService.Domain.{outboxEvent.EventType}, PetAdoption.PetService");
            if (eventType == null)
            {
                _logger.LogWarning("Could not find event type {EventType}", outboxEvent.EventType);
                return null;
            }

            // Deserialize
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
