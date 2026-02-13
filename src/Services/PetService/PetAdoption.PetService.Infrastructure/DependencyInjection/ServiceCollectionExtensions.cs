using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly? assembly)
    {
        services.AddScoped<IMediator, Mediator.Mediator>();

        var handlerInterface = typeof(IRequestHandler<,>);
        var handlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { Handler = t, Interface = i }))
            // Exclude open generics (generic type definitions)
            .Where(x => !x.Handler.IsGenericTypeDefinition);
        
        foreach (var h in handlers)
        {
            services.AddTransient(h.Interface, h.Handler);
        }

        var nonGenericHandlerInterface = typeof(IRequestHandler<>);
        var nonGenericHandlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == nonGenericHandlerInterface)
                .Select(i => new { Handler = t, Interface = i }))
            .Where(x => !x.Handler.IsGenericTypeDefinition);

        foreach (var h in nonGenericHandlers)
        {
            services.AddTransient(h.Interface, h.Handler);
        }


        /*var validatorInterface = typeof(IValidator<>);
        var validators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Validator = t, Interface = i }));

        foreach (var v in validators)
        {
            services.AddTransient(v.Interface, v.Validator);
        }
        
        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        */
        // order affects the sequence of execution
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}