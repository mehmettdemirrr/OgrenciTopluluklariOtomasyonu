namespace Core.CrossCuttingConcerns.Caching;

/// <summary>docs/MIMARI.md · A-17: referans verisi, topluluk listesi ve izin kataloğu için kullanılır.</summary>
public interface ICacheManager
{
    bool TryGet<T>(string key, out T? value);

    void Add(string key, object data, int durationMinutes);

    void Remove(string key);

    void RemoveByPattern(string pattern);
}
