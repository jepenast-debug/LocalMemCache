namespace LocalCache.Infrastructure.Resilience;

public static class RetryPolicy {
    public static async Task ExecuteAsync (Func<Task> action, int retries = 3) {
        for (int i = 0; i < retries; i++) {
            try {
                await action();
                return;
            } catch {
                if (i == retries - 1)
                    throw;

                await Task.Delay(50);
            }
        }
    }
}