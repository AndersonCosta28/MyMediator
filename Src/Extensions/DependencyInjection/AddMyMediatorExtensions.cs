using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MyMediator.Extensions.DependencyInjection;

public static class AddMyMediatorExtensions
{
    public static IServiceCollection AddMyMediator(this IServiceCollection services) => services.AddMyMediator(Assembly.GetExecutingAssembly());

    public static IServiceCollection AddMyMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            AddAssembly(services, assembly);

        services.AddScoped<IMediator, Mediator>();
        return services;
    }

    public static IServiceCollection AddMyMediator(this IServiceCollection services, params IEnumerable<Type> handlerTypes)
    {
        AddHandlerTypes(services, handlerTypes);
        services.AddScoped<IMediator, Mediator>();
        return services;
    }

    private static void AddAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(IsHandlerType))
                .ToList();

        AddHandlerTypes(services, handlerTypes);
    }

    private static void AddHandlerTypes(IServiceCollection services, params IEnumerable<Type> handlerTypes)
    {
        var grouped = handlerTypes
            .SelectMany(t => t.GetInterfaces()
            .Where(IsHandlerType)
            .Select(i => (Handler: t, Interface: i)))
            .GroupBy(x => x.Interface);

        foreach (var group in grouped)
        {
            // Allow multiple registrations only for notification handlers.
            var genericDef = group.Key.IsGenericType ? group.Key.GetGenericTypeDefinition() : null;

            if (group.Count() > 1 && genericDef != typeof(INotificationHandler<>))
                throw new InvalidOperationException($"Multiple handlers were found for request type {group.Key}. Only one IRequestHandler is allowed per request.");

            foreach (var (handler, iface) in group)
                services.AddTransient(iface, handler);
        }
    }

    /// <summary>
    /// Registers the specified pipeline behavior types in order.
    /// Supports open-generic pipeline types (e.g. typeof(ValidationBehavior&lt;,&gt;)).
    /// </summary>
    public static IServiceCollection AddPipelineBehaviors(this IServiceCollection services, params Type[] pipelineTypes)
    {
        AddPipelineTypes(services, pipelineTypes);
        return services;
    }

    private static void AddPipelineTypes(IServiceCollection services, IEnumerable<Type> pipelineTypes)
    {
        foreach (var handler in pipelineTypes)
        {
            var interfaces = handler.GetInterfaces().Where(IsPipelineBehavior).ToList();

            // If the handler is an open generic type (generic type definition), register as open-generic service
            if (handler.IsGenericTypeDefinition || handler.ContainsGenericParameters)
            {
                foreach (var iface in interfaces)
                {
                    var serviceType = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;
                    var implementationType = handler.IsGenericTypeDefinition ? handler : handler.GetGenericTypeDefinition();
                    services.AddTransient(serviceType, implementationType);
                }
            }
            else
            {
                foreach (var iface in interfaces)
                    services.AddTransient(iface, handler);
            }
        }
    }

    private static bool IsHandlerType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();

        return genericDef == typeof(IRequestHandler<,>)
            || genericDef == typeof(IRequestHandler<>)
            || genericDef == typeof(INotificationHandler<>);
    }

    private static bool IsPipelineBehavior(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();

        return genericDef == typeof(IPipelineBehavior<,>);
    }
}
