using MyMediator.Interfaces;

namespace MyMediator;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request), "A request cannot be null.");

        var requestType = request.GetType();

        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var handler = serviceProvider.GetService(handlerType) ??
            throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

        dynamic typedHandler = handler;
        await typedHandler.HandleAsync((dynamic)request, cancellationToken);
    }

    public async Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request), "A request cannot be null.");

        var requestType = request.GetType();

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));

        var handler = serviceProvider.GetService(handlerType) ??
            throw new InvalidOperationException($"No handler found for type {requestType.Name}.");

        dynamic typedHandler = handler;
        return await typedHandler.HandleAsync((dynamic)request, cancellationToken);
    }

    //private async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
    //    where TRequest : IRequest
    //{
    //    var requestType = request.GetType();
    //    var validationType = typeof(IRequestValidation<>).MakeGenericType(requestType);
    //    var validator = serviceProvider.GetService(validationType);
    //    if (validator != null)
    //    {
    //        dynamic typedValidator = validator;
    //        await typedValidator.ValidateAsync((dynamic)request, cancellationToken);
    //    }
    //}

    //private async Task ValidateAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken)
    //    where TRequest : IRequest<TResult>
    //{
    //    var requestType = request.GetType();
    //    var validationType = typeof(IRequestValidation<,>).MakeGenericType(requestType, typeof(TResult));
    //    var validator = serviceProvider.GetService(validationType);
    //    if (validator != null)
    //    {
    //        dynamic typedValidator = validator;
    //        await typedValidator.ValidateAsync((dynamic)request, cancellationToken);
    //    }
    //}
}
