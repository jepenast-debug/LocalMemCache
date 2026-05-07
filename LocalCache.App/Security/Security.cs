namespace LocalCache.Application.Security;

public class AuthAttemptTracker (int MaxAttempsAuth) {
    private class Attempt {
        public int Count;
        public DateTime LastAttempt;
        public DateTime? BlockedUntil;
    }

    private readonly Dictionary<string, Attempt> Attempts = new();

    private int MaxAttempts = MaxAttempsAuth;
    private static readonly TimeSpan BlockTime = TimeSpan.FromMinutes(5);

    public bool IsBlocked (string ClientId) {
        if (!Attempts.ContainsKey(ClientId)) return false;

        var Attempt = Attempts[ClientId];

        if (Attempt.BlockedUntil.HasValue &&
            Attempt.BlockedUntil > DateTime.UtcNow)
            return true;
        return false;
    }

    public void RegisterFailure (string ClientId) {
        if (!Attempts.ContainsKey(ClientId))
            Attempts[ClientId] = new Attempt();

        var attempt = Attempts[ClientId];

        attempt.Count++;
        attempt.LastAttempt = DateTime.UtcNow;

        if (attempt.Count >= MaxAttempts) {
            attempt.BlockedUntil = DateTime.UtcNow.Add(BlockTime);
            attempt.Count = 0;
        }
    }

    public void RegisterSuccess (string clientId) {
        Attempts.Remove(clientId);
    }
}