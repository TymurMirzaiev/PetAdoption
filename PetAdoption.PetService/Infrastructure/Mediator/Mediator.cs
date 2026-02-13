using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PetAdoption.PetService.Infrastructure.Mediator;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Send(IRequest request, CancellationToken cancellationToken = default);
}

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}

// Non-generic marker for commands without response
public interface IRequest { }

// Generic request with response
public interface IRequest<TResponse> : IRequest { }


public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken ct);
}

internal sealed class LoggingRequestHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    ILogger<LoggingRequestHandler<TRequest, TResponse>> logger) : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin pipeline behavior {Request}", request.GetType().Name);

        var response = await innerHandler.Handle(request, cancellationToken);

        logger.LogInformation("End pipeline behavior {Request}", request.GetType().Name);

        return response;
    }
}

public class Mediator(IServiceProvider provider) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = provider.GetRequiredService(handlerType);

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = (IEnumerable<dynamic>)provider.GetServices(behaviorType);

        Func<Task<TResponse>> handlerFunc = () => handler.Handle((dynamic)request, ct);
        foreach (var behavior in behaviors.Reverse())
        {
            var next = handlerFunc;
            handlerFunc = () => behavior.Handle((dynamic)request, next, ct);
        }

        return handlerFunc();
    }

    public Task Send(IRequest request, CancellationToken ct = default)
    {
        // Treat void commands as returning Unit for pipeline reuse
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        dynamic handler = provider.GetRequiredService(handlerType);

        // behaviors that were defined for generic requests are skipped here (or could be extended with non-generic pipeline)
        return handler.Handle((dynamic)request, ct);
    }
}

public readonly struct Unit { public static readonly Unit Value = new(); }