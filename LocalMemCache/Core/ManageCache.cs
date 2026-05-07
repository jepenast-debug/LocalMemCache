using LocalCache.Application;
using LocalCache.Application.AOF;
using LocalCache.Domain.Cache;
using LocalCache.Domain.General;
using LocalCache.Infrastructure;
using LocalCache.Infrastructure.Logging;
using LocalCache.Infrastructure.Metrics;
using LocalCache.Infrastructure.Services;
using System.Text;

namespace LocalMemCache.Core {
    public class ManageCache {
        private readonly Settings Cfg;
        private readonly InMemoryCacheStore Store;
        private readonly CacheService Cache;
        private readonly QuotaManager QuotaMgr;
        private readonly MetricsService Metrics;
        private readonly FileLogger Logger;
        private readonly AofService AOF;
        private readonly BackgroundCleaner Cleaner;

        public ManageCache (Settings cfg) {
            Cfg = cfg;
            Store = new(cfg.MaxKey, cfg.MaxValue);
            Cache = new(Store);
            Metrics = new();
            Logger = new(cfg.PathLog);
            QuotaMgr = new();
            AOF = new(cfg.DBPATCH + ".aof", Metrics);
            Cleaner = new(Store.InternalStore);
            _ = Task.Run(() => Cleaner.RunAsync());
        }

        public async Task<string> ExecuteCommand (string User, string Cmd, string[] Args) {
            Metrics.LogRequest();

            // Protección contra inyección / validación de tamaño básica
            if (Args.Any(a => a.Length > 50000)) return "ERR value too large";

            switch (Cmd) {
                case "GET":
                    return await GET(Args[1], User);
                case "SET":
                    if (Args.Length < 4) return "ERR syntax";
                    return await SET(User, Args[1], Args[2], int.Parse(Args[3]));
                case "DEL":
                    return await DEL(User, Args[1]);
                default:
                    return "ERR command not supported";
            }
        }

        private async Task<string> GET (string Key, string User) {
            string Response;
            try {
                string value = await Cache.Get(Key) ?? string.Empty;
                Response = value ?? "(nil)";
                Logger.Log($"Client={User} Command=GET {Key}");
                if (value != null)
                    Metrics.LogHit();
                else
                    Metrics.LogMiss();
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                Response = "ERROR";
                Metrics.LogError();
            }
            return Response;
        }

        private async Task<string> SET (string User, string Key, string Value, int ttlSec) {
            try {
                int Size = Encoding.UTF8.GetByteCount(Value);
                if (!QuotaMgr.CheckMemory(User, Size)) return "ERR memory limit";

                await Cache.Set(User, Key, Value, ttlSec);
                // Guardamos: USUARIO COMANDO LLAVE VALOR TTL
                await AOF.Append($"{User} SET {Key} {Value} {ttlSec}");
                return "OK";
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        private async Task<string> DEL (string Key, string User) {
            try {
                string Value = await GET(Key, User);
                int Size = Encoding.UTF8.GetByteCount(Value);
                await Cache.Delete(Key);
                Logger.Log($"Client={User} Command=DEL {Key}");
                QuotaMgr.ReleaseMemory(User, Size);
                return "Delete Key" + Key + "Free Size: " + Size.ToString();
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                Metrics.LogError();
                return "Error deleting Key " + Key;

            }
        }

        public async Task RestoreData () {
            string AofPath = Cfg.DBPATCH + "\\Cache.aof";
            string SnapPath = Cfg.DBPATCH + "\\Cache.snap";

            // 1. Prioridad: Snapshot (Carga masiva rápida)
            if (File.Exists(SnapPath)) {
                Logger.Log("Restaurando desde Snapshot...");
                string json = await File.ReadAllTextAsync(SnapPath);
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, CacheItem>>(json);
                if (data != null) {
                    foreach (var Item in data) {
                        await Cache.Set(Item.Value.ClientId, Item.Key,
                            Encoding.UTF8.GetString(Item.Value.Value), 0);
                    }
                }
            }

            // 2. Replay del AOF (Recupera lo último que no entró en el snapshot)
            if (File.Exists(AofPath) && !File.Exists(SnapPath)) {
                Logger.Log("Aplicando logs del AOF...");
                foreach (string line in AOF.ReadAll()) {
                    string[] p = line.Split(' ');
                    if (p.Length < 3) continue;
                    // p[0]=User, p[1]=Cmd, p[2]=Key, p[3]=Val, p[4]=TTL
                    if (p[1] == "SET") await Cache.Set(p[0], p[2], p[3], int.Parse(p[4]));
                    if (p[1] == "DEL") await Cache.Delete(p[2]);
                }
            }
        }

        public async Task CreateSnapshot () {
            try {
                var Data = Cache.GetInternalStore();
                var Json = System.Text.Json.JsonSerializer.Serialize(Data);
                await File.WriteAllTextAsync(Cfg.DBPATCH + "\\Cache.snap", Json);
                Logger.Log("Snapshot creado con éxito.");
            } catch (Exception ex) {
                Logger.Error($"Error en Snapshot: {ex.Message}");
            }
        }

        public async Task<string> Clear () {
            try {
                await Cache.ClearAll();
                return "Clear all cache";
            } catch (Exception ex) {
                Logger.Error($"Error en Snapshot: {ex.Message}");
                return "Error";
            }
        }
    }
}