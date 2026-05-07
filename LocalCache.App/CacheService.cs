using LocalCache.Domain.Cache;
using LocalCache.Infrastructure;

namespace LocalCache.Application;

public class CacheService (ICacheStore store) {
    private readonly ICacheStore Store = store;

    public async Task<string?> Get (string key) {
        var data = await Store.GetKey(key);

        return data == null ? null : System.Text.Encoding.UTF8.GetString(data);
    }

    public async Task Set (string ClientId, string key, string value, int ttlSeconds) {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        // Si el TTL es 0, asignamos 1 semana (7 días)
        ttlSeconds = ttlSeconds == 0 ? 604800 : ttlSeconds;
        await Store.SetData(ClientId, key, bytes, TimeSpan.FromSeconds(ttlSeconds));
    }

    public Task Delete (string key) {
        return Store.RemoveData(key);
    }

    public IDictionary<string, CacheItem> GetInternalStore () =>
        (Store as InMemoryCacheStore)?.GetReadOnlyStore() ?? new Dictionary<string, CacheItem>();

    public Task ClearAll () {
        return Store.ClearStore();
    }
}