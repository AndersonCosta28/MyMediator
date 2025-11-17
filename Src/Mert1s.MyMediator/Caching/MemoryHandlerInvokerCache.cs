using Microsoft.Extensions.Caching.Memory;

namespace Mert1s.MyMediator.Caching;

/// <summary>
/// Implementation of IHandlerInvokerCache backed by IMemoryCache.
/// Stores Lazy invokers in memory with configurable expiration. Uses a composite string key based on assembly-qualified names.
/// </summary>
public class MemoryHandlerInvokerCache : IHandlerInvokerCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _options;

    public MemoryHandlerInvokerCache(IMemoryCache memoryCache, MemoryCacheEntryOptions? options = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options ?? new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) };
    }

    private static string MakeKey((Type RequestType, Type ResponseType) key)
    {
        // Use assembly-qualified names to avoid collisions
        var r = key.RequestType.AssemblyQualifiedName ?? key.RequestType.FullName ?? key.RequestType.Name;
        var s = key.ResponseType.AssemblyQualifiedName ?? key.ResponseType.FullName ?? key.ResponseType.Name;
        return r + "|" + s;
    }

    public Lazy<Func<object, CancellationToken, Task<object?>>> GetOrAdd((Type RequestType, Type ResponseType) key, Func<Lazy<Func<object, CancellationToken, Task<object?>>>> valueFactory)
    {
        var cacheKey = MakeKey(key);

        if (_memoryCache.TryGetValue(cacheKey, out Lazy<Func<object, CancellationToken, Task<object?>>> existing))
            return existing;

        var created = valueFactory();
        _memoryCache.Set(cacheKey, created, _options);
        return created;
    }
}
