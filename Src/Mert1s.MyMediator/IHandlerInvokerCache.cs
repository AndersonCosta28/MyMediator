namespace Mert1s.MyMediator;

public interface IHandlerInvokerCache
{
    Lazy<Func<object, CancellationToken, Task<object?>>> GetOrAdd((Type RequestType, Type ResponseType) key, Func<Lazy<Func<object, CancellationToken, Task<object?>>>> valueFactory);
}