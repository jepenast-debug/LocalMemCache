using LocalCache.Domain.Limits;

namespace LocalCache.Application;

public class QuotaManager {
    private class Usage {
        public long MemoryUsed;
        public int Requests;
        public DateTime WindowStart = DateTime.UtcNow;
    }

    private readonly Dictionary<string, Usage> CUsage = new();
    private readonly Dictionary<string, ClientQuota> Quotas = new();


    private readonly ClientQuota DefaultQuota = new ClientQuota {
        MaxMemoryBytes = 10 * 1024 * 1024, // 10MB por defecto
        MaxReqPerMinute = 100 // 100 peticiones por minuto
    };

    public void SetQuota (string ClientId, ClientQuota Quota) {
        Quotas[ClientId] = Quota;
    }

    private ClientQuota GetClientQuota (string ClientId) {
        // Si el cliente tiene cuota, la devuelve; si no, devuelve la de por defecto
        return Quotas.TryGetValue(ClientId, out var quota) ? quota : DefaultQuota;
    }



    public bool CheckRequest (string ClientId) {
        Usage MemUsage = GetUsage(ClientId);
        ClientQuota Quota = GetClientQuota(ClientId);

        if ((DateTime.UtcNow - MemUsage.WindowStart).TotalMinutes >= 1) {
            MemUsage.Requests = 0;
            MemUsage.WindowStart = DateTime.UtcNow;
        }
        if (MemUsage.Requests >= Quota.MaxReqPerMinute) return false;
        MemUsage.Requests++;
        return true;
    }

    public bool CheckMemory (string ClientId, long Size) {
        Usage MUsage = GetUsage(ClientId);
        var Quota = GetClientQuota(ClientId);

        return (MUsage.MemoryUsed + Size) <= Quota.MaxMemoryBytes;
    }

    public void AddMemory (string ClientId, long Size) {
        GetUsage(ClientId).MemoryUsed += Size;
    }

    private Usage GetUsage (string ClientId) {
        if (!CUsage.ContainsKey(ClientId))
            CUsage[ClientId] = new Usage();
        return CUsage[ClientId];
    }

    public void ReleaseMemory (string ClientId, long Size) {
        var Usage = GetUsage(ClientId);
        Usage.MemoryUsed = Math.Max(0, Usage.MemoryUsed - Size);
    }
}