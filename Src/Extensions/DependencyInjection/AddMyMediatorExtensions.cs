using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MyMediator.Extensions.DependencyInjection;

public static class AddMyMediatorExtensions
{
    public static IServiceCollection AddMyMediator(this IServiceCollection services) => services.AddMyMediator(Assembly.GetExecutingAssembly());

    public static IServiceCollection AddMyMediator(
            this IServiceCollection services, params Assembly[] assemblies)
    {

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(isType))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(isType);

                services.AddTransient(interfaceType, handlerType);
            }
        }

        services.AddScoped<IMediator, Mediator>();
        return services;
    }

    public static IServiceCollection AddMyMediator(
    this IServiceCollection services, params IEnumerable<Type> handlerTypes)
    {

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(isType);

            services.AddTransient(interfaceType, handlerType);
        }

        services.AddScoped<IMediator, Mediator>();
        return services;
    }

    private static bool isType(Type type)
    {
        var isGenericType = type.IsGenericType;
        if (!isGenericType) 
            return false;   
            
        var isGenericTypeDefinition = type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) || type.GetGenericTypeDefinition() == typeof(IRequestHandler<>);

        return isGenericType && isGenericTypeDefinition;
    }
}
