using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Mert1s.MyMediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHandlerInvokerCache _handlerInvokerCache;
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> _behaviorTypeCache;

    public Mediator(IServiceProvider serviceProvider, MyMediatorOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // DI-first: prefer a registered IHandlerInvokerCache; fallback to default.
        _handlerInvokerCache = serviceProvider.GetService<IHandlerInvokerCache>() ?? new DefaultHandlerInvokerConcurrentCache();
        _behaviorTypeCache = options?.BehaviorTypeCacheFactory?.Invoke() ?? new ConcurrentDictionary<(Type, Type), Type>();
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(object);
        var handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var lazyInvoker = _handlerInvokerCache.GetOrAdd((requestType, responseType), () => new Lazy<Func<object, CancellationToken, Task<object?>>>(() =>
        {
            var hType = handlerInterfaceType;
            var methodInfo = hType.GetMethod("HandleAsync")!;

            return async (req, ct) =>
            {
                var handler = _serviceProvider.GetService(hType) ??
                    throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

                var invocationResult = methodInfo.Invoke(handler, new object?[] { req, ct });

                if (invocationResult is Task task)
                {
                    await task.ConfigureAwait(false);
                    // If Task<TResult>, get Result via dynamic
                    if (task.GetType().IsGenericType)
                        return ((dynamic)task).Result;

                    return null;
                }

                return invocationResult;
            };
        }, LazyThreadSafetyMode.ExecutionAndPublication));

        var invoker = lazyInvoker.Value;

        var pipeline = BuildPipeline<object>(request, () => invoker(request, cancellationToken), cancellationToken);

        await pipeline();
    }

    public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResult);
        var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));

        var lazyInvoker = _handlerInvokerCache.GetOrAdd((requestType, responseType), () => new Lazy<Func<object, CancellationToken, Task<object?>>>(() =>
        {
            var hType = handlerInterfaceType;
            var methodInfo = hType.GetMethod("HandleAsync")!;

            return async (req, ct) =>
            {
                var handler = _serviceProvider.GetService(hType) ??
                    throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

                var invocationResult = methodInfo.Invoke(handler, new object?[] { req, ct });

                if (invocationResult is Task task)
                {
                    await task.ConfigureAwait(false);
                    if (task.GetType().IsGenericType)
                        return ((dynamic)task).Result;

                    return null;
                }

                return invocationResult;
            };
        }, LazyThreadSafetyMode.ExecutionAndPublication));

        var invoker = lazyInvoker.Value;

        var result = await invoker(request, cancellationToken);
        return (TResult)result!;
    }

    private Func<Task<TResponse>> BuildPipeline<TResponse>(
        object request,
        Func<Task<TResponse>> handlerDelegate,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var behaviorType = _behaviorTypeCache.GetOrAdd((requestType, responseType), key => typeof(IPipelineBehavior<,>).MakeGenericType(key.RequestType, key.ResponseType));

        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<dynamic>().ToList();

        var pipeline = handlerDelegate;

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync((dynamic)request, cancellationToken, next);
        }

        return pipeline;
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
    where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());

        // Resolve todos os handlers registrados para esse evento
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            dynamic typedHandler = handler;
            await typedHandler.HandleAsync((dynamic)notification, cancellationToken);
        }
    }
}
