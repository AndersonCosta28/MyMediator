using System.Collections.Concurrent;

namespace Mert1s.MyMediator;

/// <summary>
/// Options to configure internal caches used by MyMediator.
/// Consumers can provide factories to create their preferred cache implementations
/// (for example a bounded cache, or a cache backed by <c>MemoryCache</c>).
/// </summary>
public class MyMediatorOptions
{
    /// <summary>
    /// Optional factory to create the behavior type cache.
    /// If not provided, a default <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> is used.
    /// Key: (RequestType, ResponseType)
    /// Value: Type
    /// </summary>
    public Func<System.Collections.Concurrent.ConcurrentDictionary<(Type RequestType, Type ResponseType), Type>>? BehaviorTypeCacheFactory { get; set; }
}