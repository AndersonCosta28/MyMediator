namespace Mert1s.MyMediator;

/// <summary>
/// Mediator entry point used to send requests and publish notifications.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Publishes a notification to all registered <see cref="INotificationHandler{TNotification}"/> instances.
    /// </summary>
    /// <typeparam name="TNotification">Notification type.</typeparam>
    /// <param name="notification">Notification instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken) where TNotification : INotification;

    /// <summary>
    /// Sends a request that expects a response of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">Response type.</typeparam>
    /// <param name="request">Request instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request that does not produce a response.
    /// </summary>
    /// <param name="request">Request instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(IRequest request, CancellationToken cancellationToken);
}