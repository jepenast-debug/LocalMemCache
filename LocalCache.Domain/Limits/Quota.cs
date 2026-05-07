namespace LocalCache.Domain.Limits;

public class ClientQuota {
    public long MaxMemoryBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxReqPerMinute { get; set; } = 1000;
}