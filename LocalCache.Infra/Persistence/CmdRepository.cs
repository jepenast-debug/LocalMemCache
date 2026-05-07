namespace LocalCache.Infrastructure.Persistence;

public class CmdRepository {

    public string CreateTable (string TableName) {
        string Result = string.Empty;
        switch (TableName) {
            case "Users":
                Result = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id TEXT PRIMARY KEY,
                            PwdHash TEXT NOT NULL,
                            IsActive INTEGER NOT NULL,
                            Profile TEXT NOT NULL
                        );";
                break;
            case "Metrics":
                Result = @"
                        CREATE TABLE IF NOT EXISTS Metrics (
                            id TEXT PRIMARY KEY,
                            Errors INTEGER,
                            CacheHits INTEGER,
                            CacheMisses INTEGER,
                            TotalReq INTEGER
                        );";
                break;
            case "UserLog":
                Result = @"
                        CREATE TABLE IF NOT EXISTS UserLog (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            User TEXT,
                            Command TEXT NOT NULL,
                            Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                            Status TEXT NOT NULL, 
                            DurationMs INTEGER NOT NULL
                        );";
                break;
            default:
                break;
        }
        return Result;
    }

    public string GetUser () {
        return "SELECT Id, PwdHash, IsActive FROM Users WHERE Id = $Id";
    }

    public string GetAddUser () {
        return @"
            INSERT INTO Users (Id, PwdHash, IsActive)
            VALUES ($id, $pass, 1)";
    }

    public string GetChagePwd () {
        return @"UPDATE Users SET PwdHash=$pass WHERE Id=$id";
    }

    public string GetUpdateStatus () {
        return "UPDATE Users SET IsActivate = $active WHERE Id = $id";
    }

    public string GetDeleteUser () {
        return "DELETE Users WHERE Id = $id";
    }

}
