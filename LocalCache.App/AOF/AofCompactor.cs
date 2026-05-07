using LocalCache.Domain.Cache;

namespace LocalCache.Application.AOF;

public class AofCompactor {
    private readonly AofService Aof;
    private readonly Func<Dictionary<string, CacheItem>> GetStore;

    public AofCompactor (AofService aof, Func<Dictionary<string, CacheItem>> getStore) {
        Aof = aof;
        GetStore = getStore;
    }

    //public async Task RunAsync (CancellationToken token) {
    //    while (!token.IsCancellationRequested) {
    //        await Task.Delay(TimeSpan.FromMinutes(5), token);

    //        try {
    //            var snapshot = GetStore();
    //            Aof.RewriteAOF(snapshot);
    //        } catch (Exception ex) {
    //            Logger.Error($"Compaction error: {ex.Message}");
    //            return ex.Message;
    //        }
    //    }
    //}
}