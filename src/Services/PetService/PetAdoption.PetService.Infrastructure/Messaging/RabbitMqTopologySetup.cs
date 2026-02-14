using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.PetService.Infrastructure.Messaging.Configuration;
using RabbitMQ.Client;

namespace PetAdoption.PetService.Infrastructure.Messaging;

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
        try
        {
            _logger.LogInformation("Setting up RabbitMQ topology...");

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup RabbitMQ topology");
            throw;
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