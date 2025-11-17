using Mert1s.MyMediator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mert1s.MyMediator.Extensions.DependencyInjection;

public static class AddMyMediatorExtensions
{
    public static IServiceCollection AddMyMediator(this IServiceCollection services)
    {
        EnsureOptions(services);
        return services.AddMyMediator(Assembly.GetCallingAssembly());
    }

    public static IServiceCollection AddMyMediator(this IServiceCollection services, Action<MyMediatorOptions> configure)
    {
        var options = new MyMediatorOptions();
        configure?.Invoke(options);

        // Register or replace configured options as singleton
        services.AddSingleton(options);

        return services.AddMyMediator();
    }

    /// <summary>
    /// Register a factory that produces an <see cref="IHandlerInvokerCache"/> and registers it as singleton.
    /// This overload also proceeds to register MyMediator scanning the calling assembly.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, Func<IServiceProvider, IHandlerInvokerCache> handlerInvokerCacheFactory)
    {
        services.AddSingleton<IHandlerInvokerCache>(sp => handlerInvokerCacheFactory(sp));
        return services.AddMyMediator();
    }

    /// <summary>
    /// Register an existing <see cref="IHandlerInvokerCache"/> instance and register MyMediator scanning the calling assembly.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, IHandlerInvokerCache handlerInvokerCache)
    {
        services.AddSingleton<IHandlerInvokerCache>(handlerInvokerCache);
        return services.AddMyMediator();
    }

    /// <summary>
    /// Register a factory that produces an <see cref="IHandlerInvokerCache"/> and register MyMediator scanning the provided assemblies.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, Func<IServiceProvider, IHandlerInvokerCache> handlerInvokerCacheFactory, params Assembly[] assemblies)
    {
        services.AddSingleton<IHandlerInvokerCache>(sp => handlerInvokerCacheFactory(sp));
        return services.AddMyMediator(assemblies);
    }

    /// <summary>
    /// Register an existing <see cref="IHandlerInvokerCache"/> instance and register MyMediator scanning the provided assemblies.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, IHandlerInvokerCache handlerInvokerCache, params Assembly[] assemblies)
    {
        services.AddSingleton<IHandlerInvokerCache>(handlerInvokerCache);
        return services.AddMyMediator(assemblies);
    }

    /// <summary>
    /// Register a factory that produces an <see cref="IHandlerInvokerCache"/> and register MyMediator registering the provided handler types.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, Func<IServiceProvider, IHandlerInvokerCache> handlerInvokerCacheFactory, params IEnumerable<Type> handlerTypes)
    {
        services.AddSingleton<IHandlerInvokerCache>(sp => handlerInvokerCacheFactory(sp));
        return services.AddMyMediator(handlerTypes);
    }

    /// <summary>
    /// Register an existing <see cref="IHandlerInvokerCache"/> instance and register MyMediator registering the provided handler types.
    /// </summary>
    public static IServiceCollection AddMyMediator(this IServiceCollection services, IHandlerInvokerCache handlerInvokerCache, params IEnumerable<Type> handlerTypes)
    {
        services.AddSingleton<IHandlerInvokerCache>(handlerInvokerCache);
        return services.AddMyMediator(handlerTypes);
    }

    public static IServiceCollection AddMyMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        EnsureOptions(services);

        foreach (var assembly in assemblies)
            AddAssembly(services, assembly);

        // Register factory for Mediator using options resolved from service provider
        services.AddScoped<IMediator>(sp => new Mediator(sp, sp.GetService<MyMediatorOptions>()));
        return services;
    }

    public static IServiceCollection AddMyMediator(this IServiceCollection services, params IEnumerable<Type> handlerTypes)
    {
        EnsureOptions(services);

        AddHandlerTypes(services, handlerTypes);
        services.AddScoped<IMediator>(sp => new Mediator(sp, sp.GetService<MyMediatorOptions>()));
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
    /// Registers the specified pipeline behavior types in the order provided.
    /// Supports open-generic pipeline types (for example <c>typeof(ValidationBehavior&lt;,&gt;)</c>).
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="pipelineTypes">Pipeline behavior types to register.</param>
    /// <returns>The same service collection for chaining.</returns>
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
                foreach (var iface in interfaces)
                {
                    var serviceType = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;
                    var implementationType = handler.IsGenericTypeDefinition ? handler : handler.GetGenericTypeDefinition();
                    services.AddTransient(serviceType, implementationType);
                }
            else
                foreach (var iface in interfaces)
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

    private static bool IsPipelineBehavior(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();

        return genericDef == typeof(IPipelineBehavior<,>);
    }

    private static void EnsureOptions(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(MyMediatorOptions)))
        {
            services.AddSingleton(new MyMediatorOptions());
        }
    }
}
