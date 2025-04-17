namespace MyMediator.Interfaces;
public interface IRequestValidation<in TRequest>
    where TRequest : IRequest
{
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}
public interface IRequestValidation<in TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}