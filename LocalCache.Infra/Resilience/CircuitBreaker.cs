namespace LocalCache.Infrastructure.Resilience;

public class CircuitBreaker (int threshold = 5, int seconds = 30) {
    private int Failures;
    private readonly int Threshold = threshold;
    private readonly TimeSpan OpenTime = TimeSpan.FromSeconds(seconds);

    private DateTime? BlockedUntil;

    public bool IsOpen () {
        if (BlockedUntil == null) return false;

        if (DateTime.UtcNow > BlockedUntil) {
            Failures = 0;
            BlockedUntil = null;
            return false;
        }

        return true;
    }

    public void RegisterFailure () {
        Failures++;

        if (Failures >= Threshold) {
            BlockedUntil = DateTime.UtcNow.Add(OpenTime);
        }
    }

    public void RegisterSuccess () {
        Failures = 0;
        BlockedUntil = null;
    }
}