namespace MyMediator;

public interface IMediator
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken) where TNotification : INotification;
    Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken);

    Task SendAsync(IRequest request, CancellationToken cancellationToken);
}