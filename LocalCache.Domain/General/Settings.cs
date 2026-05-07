namespace LocalCache.Domain.General {
    public class Settings {
        public int PORT { get; set; }
        public bool AUTH { get; set; }
        public string DBPATCH { get; set; }
        public int RATELIMIT { get; set; }
        public string PathLog { get; set; }
        public int MaxKey { get; set; }
        public int MaxValue { get; set; }
        public int MaxAuth { get; set; }
        public int MaxReqPerMinute { get; set; }
        public int MaxMemoryBytes { get; set; }

    }
}
