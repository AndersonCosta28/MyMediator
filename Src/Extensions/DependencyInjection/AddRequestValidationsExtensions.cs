using Microsoft.Extensions.DependencyInjection;
using MyMediator.Interfaces;
using System.Reflection;

namespace MyMediator.Extensions.DependencyInjection;
public static class AddRequestValidationsExtensions
{
    public static IServiceCollection AddRequestValidations(
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

                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }

    private static bool isType(Type type)
    {
        var isGenericType = type.IsGenericType;
        var isGenericTypeDefinition = type.GetGenericTypeDefinition() == typeof(IRequestValidation<,>) || type.GetGenericTypeDefinition() == typeof(IRequestValidation<>);

        return isGenericType && isGenericTypeDefinition;
    }
}
