using LocalCache.Domain.Cache;
using System.Collections.Concurrent;

namespace LocalCache.Infrastructure.Services;

public class BackgroundCleaner {
    private readonly ConcurrentDictionary<string, CacheItem> Store;

    public BackgroundCleaner (ConcurrentDictionary<string, CacheItem> store) {
        Store = store;
    }

    public async Task RunAsync () {
        while (true) {
            foreach (var kv in Store) {
                if (kv.Value.IsExpired()) {
                    Store.TryRemove(kv.Key, out var item);
                }
            }
            await Task.Delay(10000);
        }
    }

    public void RewriteAOF (string FilePath, Dictionary<string, CacheItem> store) {
        try {
            string TempFile = FilePath + ".tmp";
            using (var writer = new StreamWriter(TempFile, false)) {
                foreach (var kv in store) {
                    var value = Convert.ToBase64String(kv.Value.Value);
                    writer.WriteLine($"SET {kv.Key} {value} 0");
                }
            }

            File.Move(TempFile, FilePath, true);
        } catch (Exception ex) {
            Console.WriteLine($"AOF Rewrite Error: {ex.Message}");
        }
    }
}