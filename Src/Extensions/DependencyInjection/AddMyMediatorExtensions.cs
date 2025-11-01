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
            if (group.Count() > 1 && group.Key.GetGenericTypeDefinition() != typeof(INotificationHandler<>))
                throw new InvalidOperationException($"Multiple handlers were found for request type {group.Key}. Only one IRequestHandler is allowed per request.");

            foreach (var (handler, iface) in group)
                services.AddTransient(iface, handler);
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
}
