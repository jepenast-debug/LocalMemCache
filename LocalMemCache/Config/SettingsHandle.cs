using LocalCache.Domain.General;
using LocalCache.Infrastructure.Logging;
using System.Configuration;

namespace LocalMemCache.Configuration;

public class ConfigReader : ISettingsLoad {

    public Settings LoadSettings () {
        return new Settings {
            PORT = GetInt("Port", 6379),
            AUTH = GetBool("Auth", true),
            DBPATCH = GetString("DBPath", ".\\db\\LocalCache.db"),
            RATELIMIT = GetInt("RateLimit", 1000),
            PathLog = GetString("ActionLog", ".\\Log"),
            MaxKey = GetInt("MaxKeyLenght", 256),
            MaxValue = GetInt("MaxValueSize", 124 * 1024),
            MaxAuth = GetInt("MaxAuthAttemps", 3),
            MaxReqPerMinute = GetInt("MaxReqPerMinute", 100),
            MaxMemoryBytes = GetInt("MaxMemoryBytes", 1297920)
        };
    }

    public string GetString (string key, string DefaultValue) {
        return ConfigurationManager.AppSettings[key] ?? DefaultValue;
    }

    public int GetInt (string key, int defaultValue) {
        var value = ConfigurationManager.AppSettings[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public bool GetBool (string key, bool defaultValue) {
        var value = ConfigurationManager.AppSettings[key];
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public bool SetSetting (string Key, string Value) {
        try {
            ConfigurationManager.AppSettings.Set(Key, Value);
            new FileLogger(LoadSettings().PathLog).Log("Cambio del parametro: " + Key);
            return true;
        } catch (Exception Ex) {
            new FileLogger(LoadSettings().PathLog).Error(Ex.Message);
            return false;
        }

    }
}