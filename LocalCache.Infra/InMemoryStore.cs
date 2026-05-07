using LocalCache.Domain.Cache;
using System.Collections.Concurrent;

namespace LocalCache.Infrastructure;

/// <summary>Agrega, Elimina y lee los datos de memoria</summary>
public class InMemoryCacheStore (int MaxKey, int MaxValue) : ICacheStore {

    public readonly ConcurrentDictionary<string, CacheItem> Store = new();
    public ConcurrentDictionary<string, CacheItem> InternalStore => Store;

    private readonly int MaxKeyLength = MaxKey;
    private readonly int MaxValueSize = MaxValue;
    public long Hits = 0;
    public long Misses = 0;

    public Task<byte[]?> GetKey (string key) {
        ValidateKey(key);
        if (Store.TryGetValue(key, out var item)) {
            if (item.IsExpired()) {
                Store.TryRemove(key, out _);
                Misses++;
                return Task.FromResult<byte[]?>(null);
            }
            item.LastAccess = DateTime.UtcNow;
            Hits++;
            return Task.FromResult<byte[]?>(item.Value);
        }
        return Task.FromResult<byte[]?>(null);
    }

    public Task SetData (string ClientId, string Key, byte[] value, TimeSpan? ttl = null) {
        ValidateKey(Key);
        ValidateValue(value);

        int Size = value.Length;

        CacheItem Item = new() {
            Value = value,
            Size = Size,
            ClientId = ClientId,
            Expiration = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
        };

        Store[Key] = Item;
        EnforceLimit();
        return Task.CompletedTask;
    }


    public Task RemoveData (string key) {
        try {
            ValidateKey(key);
            Store.TryRemove(key, out var item);
            return Task.CompletedTask;
        } catch (Exception ex) {
            return Task.FromException(ex);
        }
    }

    public Task ClearStore () {
        Store.Clear();
        return Task.CompletedTask;
    }

    //Valdia que cumpla con el tamaño esperado
    private void ValidateKey (string key) {
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
            throw new ArgumentException("Invalid key, Max lenght size");
    }

    private void ValidateValue (byte[] value) {
        if (value == null || value.Length == 0 || value.Length > MaxValueSize)
            throw new ArgumentException("Invalid value Max lenght size");
    }

    private void EnforceLimit () {
        if (Store.Count < 10000) return;
        var OldEst = Store.OrderBy(x => x.Value.LastAccess).First();
        Store.TryRemove(OldEst.Key, out _);
    }

    public IDictionary<string, CacheItem> GetReadOnlyStore () {
        // Retornamos una copia o el enumerador para evitar bloqueos prolongados
        return Store.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}