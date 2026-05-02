using Microsoft.Extensions.DependencyInjection;
using PetAdoption.PetService.Application.Abstractions;

namespace PetAdoption.PetService.Infrastructure.Mediator;

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