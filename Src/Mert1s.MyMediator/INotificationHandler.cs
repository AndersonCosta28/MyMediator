namespace Mert1s.MyMediator;

/// <summary>
/// Handler for notifications of type <typeparamref name="TNotification"/>.
/// </summary>
/// <typeparam name="TNotification">Notification type.</typeparam>
public interface INotificationHandler<TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the notification asynchronously.
    /// </summary>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}