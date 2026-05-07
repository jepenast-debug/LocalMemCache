namespace LocalCache.Domain.Cache;

public class CacheItem {
    public byte[] Value { get; init; } = Array.Empty<byte>();
    public DateTime? Expiration { get; init; }

    public DateTime LastAccess { get; set; } = DateTime.UtcNow;

    public bool IsExpired () {
        return Expiration.HasValue && DateTime.UtcNow > Expiration.Value;
    }

    public long Size { get; set; }
    public string ClientId { get; set; } = "";
}