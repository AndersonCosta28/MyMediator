namespace Mert1s.MyMediator;

/// <summary>
/// Handler for requests that return a result.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResult">Response type.</typeparam>
public interface IRequestHandler<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    /// <summary>
    /// Handles the request asynchronously and returns a response.
    /// </summary>
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Handler for requests that do not return a result.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Handles the request asynchronously.
    /// </summary>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}
