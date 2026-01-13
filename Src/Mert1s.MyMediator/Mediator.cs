using Microsoft.Extensions.DependencyInjection;

namespace Mert1s.MyMediator;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var handler = serviceProvider.GetService(handlerType) ??
            throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

        // Monta pipeline para void (sem retorno)
        var method = handlerType.GetMethod("HandleAsync");
        var pipeline = this.BuildPipeline<object>(request, cancellationToken, async () =>
        {
            await (Task)method!.Invoke(handler, [request, cancellationToken])!;
            return null!;
        });

        await pipeline();
    }

    public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));

        var handler = serviceProvider.GetService(handlerType) ??
            throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

        // Monta pipeline para requests com retorno
        var method = handlerType.GetMethod("HandleAsync");
        var pipeline = this.BuildPipeline(request, cancellationToken, () => (Task<TResult>)method!.Invoke(handler, new object[] { request, cancellationToken })!);

        return await pipeline();
    }

    private Func<Task<TResponse>> BuildPipeline<TResponse>(
        object request,
        CancellationToken cancellationToken,
        Func<Task<TResponse>> handlerDelegate)
    {
        var requestType = request.GetType();
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));

        var behaviors = serviceProvider.GetServices(behaviorType).Cast<dynamic>().ToList();

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
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            await (Task)method!.Invoke(handler, [notification, cancellationToken])!;
        }
    }
}
