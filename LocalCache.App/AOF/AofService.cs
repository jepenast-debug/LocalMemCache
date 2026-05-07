using LocalCache.Infrastructure.Metrics;
using LocalCache.Infrastructure.Resilience;

namespace LocalCache.Application.AOF;

public class AofService {
    private readonly string FilePath;
    private readonly object Lock = new();
    private readonly CircuitBreaker CB = new();
    private readonly MetricsService Metrics;



    public AofService (string path, MetricsService metrics) {
        FilePath = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Metrics = metrics;
    }

    public async Task Append (string command) {
        if (CB.IsOpen())
            return;
        try {
            await RetryPolicy.ExecuteAsync(async () => {
                await File.AppendAllTextAsync(FilePath, command + Environment.NewLine);
            });

            CB.RegisterSuccess();
        } catch {
            CB.RegisterFailure();
            Metrics.LogError();
        }
    }

    public IEnumerable<string> ReadAll () {
        if (!File.Exists(FilePath))
            yield break;

        foreach (var line in File.ReadLines(FilePath))
            yield return line;
    }
}