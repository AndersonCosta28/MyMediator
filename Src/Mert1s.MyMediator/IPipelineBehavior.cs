namespace Mert1s.MyMediator;

/// <summary>
/// Pipeline behavior that can wrap request handling with additional logic (e.g., validation, logging).
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Executes behavior logic and invokes the next delegate in the pipeline.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next);
}