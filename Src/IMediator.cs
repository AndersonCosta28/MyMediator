namespace MyMediator;

public interface IMediator
{
    Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken);

    Task SendAsync(IRequest request, CancellationToken cancellationToken);
}