using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace PetAdoption.UserService.Infrastructure.Messaging;

public class RabbitMqTopologyBuilder
{
    private readonly List<Func<IChannel, Task>> _setupActions = new();

    public RabbitMqTopologyBuilder DeclareExchange(
        string name,
        string type = ExchangeType.Topic,
        bool durable = true,
        bool autoDelete = false)
    {
        _setupActions.Add(async channel =>
        {
            try
            {
                await channel.ExchangeDeclareAsync(name, type, durable, autoDelete);
            }
            catch (OperationInterruptedException ex) when (ex.ShutdownReason.ReplyCode == 406)
            {
                // PRECONDITION_FAILED - exchange exists with different properties
                // Delete and recreate with new configuration
                await channel.ExchangeDeleteAsync(name);
                await channel.ExchangeDeclareAsync(name, type, durable, autoDelete);
            }
        });
        return this;
    }

    public RabbitMqTopologyBuilder DeclareQueue(
        string name,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        Dictionary<string, object>? arguments = null)
    {
        _setupActions.Add(async channel =>
        {
            try
            {
                await channel.QueueDeclareAsync(name, durable, exclusive, autoDelete, arguments);
            }
            catch (OperationInterruptedException ex) when (ex.ShutdownReason.ReplyCode == 406)
            {
                // PRECONDITION_FAILED - queue exists with different properties
                // Delete and recreate with new configuration
                await channel.QueueDeleteAsync(name);
                await channel.QueueDeclareAsync(name, durable, exclusive, autoDelete, arguments);
            }
        });
        return this;
    }

    public RabbitMqTopologyBuilder BindQueue(
        string queue,
        string exchange,
        string routingKey = "")
    {
        _setupActions.Add(async channel =>
        {
            await channel.QueueBindAsync(queue, exchange, routingKey);
        });
        return this;
    }

    public RabbitMqTopologyBuilder FromConfiguration(RabbitMqOptions options)
    {
        foreach (var exchange in options.Exchanges)
        {
            DeclareExchange(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete);
        }

        foreach (var queue in options.Queues)
        {
            DeclareQueue(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);

            foreach (var binding in queue.Bindings)
            {
                BindQueue(queue.Name, binding.Exchange, binding.RoutingKey);
            }
        }

        return this;
    }

    public async Task SetupAsync(IChannel channel)
    {
        foreach (var action in _setupActions)
        {
            await action(channel);
        }
    }
}
