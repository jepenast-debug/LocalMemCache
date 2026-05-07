namespace LocalCache.Infrastructure.Metrics;

public class MetricsService {
    public long TotalRequests;
    public long Errors;
    public long CacheHits;
    public long CacheMisses;

    public void LogRequest () => Interlocked.Increment(ref TotalRequests);
    public void LogError () => Interlocked.Increment(ref Errors);
    public void LogHit () => Interlocked.Increment(ref CacheHits);
    public void LogMiss () => Interlocked.Increment(ref CacheMisses);

    public string Snapshot () {
        return $"Requests={TotalRequests}, Errors={Errors}, Hits={CacheHits}, Misses={CacheMisses}";
    }
}