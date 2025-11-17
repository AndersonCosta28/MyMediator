using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Mert1s.MyMediator.Caching;

/// <summary>
/// Simple adapter that exposes a ConcurrentDictionary-like API backed by MemoryCache.
/// This is an example; a production implementation should handle eviction, size limits and expiration appropriately.
/// </summary>
public class MemoryCacheAdapter<TKey, TValue>
    where TKey : notnull
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _entryOptions;

    public MemoryCacheAdapter(IMemoryCache memoryCache, MemoryCacheEntryOptions? entryOptions = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _entryOptions = entryOptions ?? new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
    }

    public TValue GetOrCreate(TKey key, Func<TValue> factory)
    {
        if (_memoryCache.TryGetValue(key, out TValue? existing))
            return existing!;

        var value = factory();
        _memoryCache.Set(key, value, _entryOptions);
        return value;
    }
}
