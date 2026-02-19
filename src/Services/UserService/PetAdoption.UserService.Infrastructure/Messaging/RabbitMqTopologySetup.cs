using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using RabbitMQ.Client;

namespace PetAdoption.UserService.Infrastructure.Messaging;

public class RabbitMqTopologySetup : IHostedService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqTopologySetup> _logger;
    private IConnection? _connection;

    public RabbitMqTopologySetup(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqTopologySetup> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        const int delayMs = 2000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Setting up RabbitMQ topology (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);

                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.User,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                var builder = new RabbitMqTopologyBuilder()
                    .FromConfiguration(_options);

                await builder.SetupAsync(channel);

                await channel.CloseAsync(cancellationToken);

                _logger.LogInformation(
                    "RabbitMQ topology setup completed. Exchanges: {ExchangeCount}, Queues: {QueueCount}",
                    _options.Exchanges.Count,
                    _options.Queues.Count);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Failed to setup RabbitMQ topology (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms...",
                    attempt, maxRetries, delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup RabbitMQ topology after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}
