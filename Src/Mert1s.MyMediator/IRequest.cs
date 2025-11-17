namespace Mert1s.MyMediator;

/// <summary>
/// Marker interface for a request that does not produce a response.
/// Implement this on types that represent commands/requests with no return value.
/// </summary>
public interface IRequest { }

/// <summary>
/// Marker interface for a request that produces a response of type <typeparamref name="TResult"/>.
/// Implement this on types that represent queries or requests that return a value.
/// </summary>
public interface IRequest<TResult> { }