using System.Collections.Concurrent;

namespace Mert1s.MyMediator;

internal sealed class DefaultHandlerInvokerConcurrentCache : IHandlerInvokerCache
{
    private readonly ConcurrentDictionary<(Type, Type), Lazy<Func<object, CancellationToken, Task<object?>>>> _inner = new();

    public Lazy<Func<object, CancellationToken, Task<object?>>> GetOrAdd((Type RequestType, Type ResponseType) key, Func<Lazy<Func<object, CancellationToken, Task<object?>>>> valueFactory)
    {
        return _inner.GetOrAdd(key, _ => valueFactory());
    }
}