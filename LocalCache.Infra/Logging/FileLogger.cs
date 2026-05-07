namespace LocalCache.Infrastructure.Logging;

public class FileLogger {
    private readonly string PathFile;

    public FileLogger (string path) {
        PathFile = path;
        Directory.CreateDirectory(PathFile);
    }

    public void Log (string StrLog) {
        var FileLog = Path.Combine(PathFile, $"log_{DateTime.UtcNow:yyyyMMdd}.txt");

        var Line = $"{DateTime.UtcNow:o} | {StrLog}";
        try {
            File.AppendAllText(FileLog, Line + Environment.NewLine);
        } catch {
            // NUNCA romper por logging
        }
    }

    public void Error (string FileLog) {
        var file = Path.Combine(PathFile, $"error_{DateTime.UtcNow:yyyyMMdd}.txt");

        var line = $"{DateTime.UtcNow:o} | ERROR | {FileLog}";
        File.AppendAllText(file, line + Environment.NewLine);
    }
}