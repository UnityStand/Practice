using Events.Application.Abstractions;

namespace Events.Tests.Fakes;

// Кеш в памяти вместо Redis: хранит значение вместе с TTL, чтобы тесты могли проверить, что и на сколько закешировано.
public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, (object Value, TimeSpan Ttl)> _entries = [];

    public bool Contains(string key) => _entries.ContainsKey(key);

    public TimeSpan TtlOf(string key) => _entries[key].Ttl;

    public T? Peek<T>(string key) where T : class =>
        _entries.TryGetValue(key, out var entry) ? (T)entry.Value : null;

    public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult(Peek<T>(key));

    public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        _entries[key] = (value, ttl);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _entries.Remove(key);
        return Task.CompletedTask;
    }
}
