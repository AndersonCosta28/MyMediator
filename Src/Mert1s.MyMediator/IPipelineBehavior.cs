namespace Mert1s.MyMediator;

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next);
}