namespace LocalCache.Domain.Entities {
    public class ClientInfo {
        public string ClientId { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public DateTime StartConn { get; set; } = DateTime.Now;
    }
}
