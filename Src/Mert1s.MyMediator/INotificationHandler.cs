namespace Mert1s.MyMediator;
public interface INotificationHandler<TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}