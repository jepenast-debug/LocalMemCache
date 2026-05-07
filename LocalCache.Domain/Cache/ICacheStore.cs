namespace LocalCache.Domain.Cache;

public interface ICacheStore {
    Task<byte[]?> GetKey (string key);
    Task SetData (string ClientId, string key, byte[] value, TimeSpan? ttl = null);
    Task RemoveData (string key);
    Task ClearStore ();
}