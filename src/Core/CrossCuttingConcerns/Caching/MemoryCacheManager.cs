using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Core.CrossCuttingConcerns.Caching;

/// <summary>
/// docs/MIMARI.md · A-54: önbellek sınırlıdır. Cache anahtarına serbest metin (`search`) girdiği için
/// hem <see cref="IMemoryCache"/> hem de anahtar defteri sınırsız büyüyebilirdi — girdiler
/// <c>Size = 1</c> ile sayılır ve tahliye edilen anahtar defterden geri çağrıyla silinir.
/// </summary>
public sealed class MemoryCacheManager(IMemoryCache cache) : ICacheManager
{
    // IMemoryCache anahtarlarını dışarı açmadığı için RemoveByPattern kendi anahtar defterini tutar.
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public bool TryGet<T>(string key, out T? value)
    {
        if (cache.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Add(string key, object data, int durationMinutes)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(durationMinutes),
            Size = 1,
        };

        // Süresi dolan/tahliye edilen girdi defterde kalırsa defter sızıntıya döner (A-54).
        options.RegisterPostEvictionCallback(static (evictedKey, _, _, state) =>
            ((ConcurrentDictionary<string, byte>)state!).TryRemove((string)evictedKey, out _), _keys);

        cache.Set(key, data, options);
        _keys.TryAdd(key, 0);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPattern(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        foreach (var key in _keys.Keys.Where(k => regex.IsMatch(k)).ToList())
        {
            Remove(key);
        }
    }
}
